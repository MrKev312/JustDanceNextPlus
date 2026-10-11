using JustDanceServer.Configuration;

using Microsoft.Extensions.Options;

using System.Text.Json.Nodes;

namespace JustDanceServer.Services;

public interface IJduCatalogService : ILoadService
{
    bool TryCreateCatalog(string family, string? skuId, out JsonObject catalog);
    bool TryCreateLegacyAvatarCatalog(string? skuId, out JsonObject catalog);
}

public static class JduCatalogFamilies
{
    public const string Items = "items";
    public const string Aliases = "aliases";
    public const string Quests = "quests";
    public const string SessionQuests = "session-quests";
    public const string CarouselAvatars = "carousel-avatars";
    public const string CarouselDancerProfile = "carousel-dancerprofile";
    public const string CarouselQuests = "carousel-quests";
}

public sealed class JduCatalogService(
    IOptions<PathSettings> pathOptions,
    IOptions<UrlSettings> urlOptions,
    IMapService mapService,
    IJduAvatarCatalog avatarCatalog,
    JduAvatarWireProjection avatarProjection,
    IJduCustomizableItemCatalog customizableItemCatalog,
    IJduAliasCatalog aliasCatalog,
    IJduQuestCatalog questCatalog,
    JduCatalogWireProjection catalogProjection,
    ILogger<JduCatalogService> logger) : IJduCatalogService
{
    public const string LocalContentUrlPrefix = "jdu-content://";
    public const string PlatformPlaceholder = "{platform}";

    private readonly Lock loadLock = new();
    private readonly PathSettings paths = pathOptions.Value;
    private readonly UrlSettings urls = urlOptions.Value;
    private Task? loadTask;
    private IReadOnlyDictionary<CatalogFragmentKey, JsonObject> fragments =
        new Dictionary<CatalogFragmentKey, JsonObject>();

    public Task LoadData()
    {
        lock (loadLock)
            return loadTask ??= LoadDataCore();
    }

    public bool TryCreateCatalog(string family, string? skuId, out JsonObject catalog)
    {
        EnsureLoaded();

        string normalizedFamily = NormalizeFamily(family);
        List<JsonObject> applicable = [];
        AddFragment(applicable, new(normalizedFamily, CatalogScope.Common));
        string? assetPlatform = null;

        if (JduMediaClientProfiles.TryResolve(skuId, out JduMediaClientProfile? client))
        {
            assetPlatform = client.AssetPlatform;
            AddFragment(applicable, new(normalizedFamily, CatalogScope.ForYear(client.GameYear)));
            AddFragment(applicable, new(
                normalizedFamily,
                CatalogScope.ForPlatform(client.GameYear, client.AssetPlatform)));
        }

        if (applicable.Count == 0)
        {
            catalog = [];
            return false;
        }

        JsonObject merged = [];
        foreach (JsonObject fragment in applicable)
            MergeObjects(merged, fragment);
        switch (normalizedFamily)
        {
            case JduCatalogFamilies.Items:
                merged["avatars"] = avatarProjection.Create(avatarCatalog.Avatars);
                merged["skins"] = catalogProjection.CreateSkins(customizableItemCatalog.Skins);
                merged["portraitBorders"] = catalogProjection.CreatePortraitBorders(
                    customizableItemCatalog.PortraitBorders);
                break;
            case JduCatalogFamilies.Aliases:
                merged["aliases"] = catalogProjection.CreateAliases(aliasCatalog.Aliases);
                break;
            case JduCatalogFamilies.Quests:
                merged["quests"] = catalogProjection.CreateQuests(questCatalog.Quests, assetPlatform);
                break;
            case JduCatalogFamilies.CarouselQuests:
                catalogProjection.PopulateQuestCarousel(merged, questCatalog.Quests);
                break;
        }

        ResolveLocalContentUrls(merged, assetPlatform);
        catalog = merged;
        return true;
    }

    public bool TryCreateLegacyAvatarCatalog(string? skuId, out JsonObject catalog)
    {
        if (!TryCreateCatalog(JduCatalogFamilies.Items, skuId, out JsonObject items)
            || items["avatars"] is not JsonObject avatars)
        {
            catalog = [];
            return false;
        }

        catalog = new JsonObject
        {
            ["__class"] = "OnlineAvatarDb",
            ["avatars"] = avatars.DeepClone()
        };
        return true;
    }

    private async Task LoadDataCore()
    {
        string root = paths.JduCatalogPath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            logger.LogWarning(
                "JDU catalog path '{CatalogPath}' does not exist; catalog endpoints will return empty databases.",
                root);
            fragments = new Dictionary<CatalogFragmentKey, JsonObject>();
            return;
        }

        await Task.WhenAll(
            avatarCatalog.LoadData(),
            customizableItemCatalog.LoadData(),
            aliasCatalog.LoadData(),
            questCatalog.LoadData());

        Dictionary<CatalogFragmentKey, JsonObject> loaded = [];
        HashSet<string> knownMapNames = new(mapService.MapToGuid.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (string familyDirectory in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
        {
            string family = NormalizeFamily(Path.GetFileName(familyDirectory));
            foreach (string scopeDirectory in Directory.EnumerateDirectories(familyDirectory).Order(StringComparer.Ordinal))
            {
                CatalogScope scope = CatalogScope.Parse(Path.GetFileName(scopeDirectory));
                CatalogFragmentKey key = new(family, scope);
                loaded.Add(key, await LoadScope(scopeDirectory, knownMapNames));
            }
        }

        fragments = loaded;
        logger.LogInformation(
            "Loaded {FragmentCount} JDU catalog scopes across {FamilyCount} families.",
            loaded.Count,
            loaded.Keys.Select(key => key.Family).Distinct(StringComparer.Ordinal).Count());
    }

    private async Task<JsonObject> LoadScope(
        string scopeDirectory,
        IReadOnlySet<string> knownMapNames)
    {
        string[] files = [.. Directory
            .EnumerateFiles(scopeDirectory, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)];
        if (files.Length == 0)
            throw new InvalidDataException($"JDU catalog scope '{scopeDirectory}' contains no JSON files.");

        JsonObject combined = [];
        foreach (string file in Directory
            .EnumerateFiles(scopeDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal))
        {
            JsonNode document = await ParseCatalogNode(file);
            if (document is not JsonObject rootFragment)
                throw new InvalidDataException($"JDU catalog root fragment '{file}' must contain a JSON object.");

            ValidateCatalogNode(rootFragment, file, knownMapNames);
            MergeObjects(combined, rootFragment);
        }

        foreach (string collectionDirectory in Directory
            .EnumerateDirectories(scopeDirectory)
            .Order(StringComparer.Ordinal))
        {
            string collectionName = Path.GetFileName(collectionDirectory);
            JsonNode collection = combined[collectionName]
                ?? throw new InvalidDataException(
                    $"Loose JDU catalog collection '{collectionDirectory}' is not declared by a root JSON file.");
            HashSet<string> looseEntryNames = [with(StringComparer.OrdinalIgnoreCase)];

            foreach (string file in Directory
                .EnumerateFiles(collectionDirectory, "*.json", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal))
            {
                JsonNode entry = await ParseCatalogNode(file);
                ValidateCatalogNode(entry, file, knownMapNames);
                AddLooseEntry(collection, file, entry, looseEntryNames);
            }
        }

        return combined;
    }

    private static async Task<JsonNode> ParseCatalogNode(string file)
        => JsonNode.Parse(await File.ReadAllTextAsync(file))
            ?? throw new InvalidDataException($"JDU catalog JSON '{file}' contains null.");

    private static void AddLooseEntry(
        JsonNode collection,
        string file,
        JsonNode entry,
        ISet<string> looseEntryNames)
    {
        if (collection is JsonArray array)
        {
            array.Add(entry);
            return;
        }

        if (collection is not JsonObject dictionary)
            throw new InvalidDataException(
                $"Loose JDU catalog entry '{file}' targets a collection that is neither an object nor an array.");

        string key = Path.GetFileNameWithoutExtension(file);
        if (!looseEntryNames.Add(key))
            throw new InvalidDataException(
                $"Loose JDU catalog entry '{file}' duplicates key '{key}'.");

        if (!dictionary.TryGetPropertyValue(key, out JsonNode? rootOverride))
        {
            dictionary.Add(key, entry);
            return;
        }

        if (entry is not JsonObject looseObject || rootOverride is not JsonObject overrideObject)
        {
            throw new InvalidDataException(
                $"Loose JDU catalog entry '{file}' cannot merge root override key '{key}' because both values must be objects.");
        }

        JsonObject merged = (JsonObject)looseObject.DeepClone();
        MergeObjects(merged, overrideObject);
        dictionary[key] = merged;
    }

    private void EnsureLoaded()
    {
        Task task = LoadData();
        if (!task.IsCompletedSuccessfully)
            task.GetAwaiter().GetResult();
    }

    private void AddFragment(List<JsonObject> destination, CatalogFragmentKey key)
    {
        if (fragments.TryGetValue(key, out JsonObject? fragment))
            destination.Add(fragment);
    }

    private void ValidateLocalContentUrls(JsonNode node, string sourceFile)
    {
        foreach (string url in EnumerateStrings(node).Where(value =>
            value.StartsWith(LocalContentUrlPrefix, StringComparison.Ordinal)))
        {
            IEnumerable<string> resolvedUrls = url.Contains(PlatformPlaceholder, StringComparison.Ordinal)
                ? JduAssetPlatform.SupportedPlatforms.Select(platform =>
                    url.Replace(PlatformPlaceholder, platform, StringComparison.Ordinal))
                : [url];

            foreach (string resolvedUrl in resolvedUrls)
            {
                string relativePath = GetSafeLocalRelativePath(resolvedUrl, sourceFile);
                string physicalPath = Path.Combine(paths.JduCatalogAssetsPath, relativePath);
                if (!File.Exists(physicalPath))
                {
                    throw new FileNotFoundException(
                        $"JDU catalog '{sourceFile}' references missing local content '{resolvedUrl}' from '{url}'.",
                        physicalPath);
                }
            }
        }
    }

    private void ValidateCatalogNode(
        JsonNode node,
        string sourceFile,
        IReadOnlySet<string> knownMapNames)
    {
        ValidateLocalContentUrls(node, sourceFile);

        foreach ((string MapName, string Field) in EnumerateMapReferences(node))
        {
            if (!knownMapNames.Contains(MapName))
            {
                logger.LogWarning(
                    "JDU catalog '{SourceFile}' references unknown map '{MapName}' in field '{Field}'.",
                    sourceFile,
                    MapName,
                    Field);
            }
        }
    }

    private static IEnumerable<(string MapName, string Field)> EnumerateMapReferences(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach ((string propertyName, JsonNode? value) in jsonObject)
            {
                if (value is JsonValue jsonValue
                    && IsMapNameField(propertyName)
                    && jsonValue.TryGetValue(out string? mapName)
                    && !string.IsNullOrWhiteSpace(mapName))
                {
                    yield return (mapName, propertyName);
                }
                else if (value is JsonArray mapNames
                    && IsMapNameListField(propertyName))
                {
                    foreach (JsonNode? item in mapNames)
                    {
                        if (item is JsonValue mapValue
                            && mapValue.TryGetValue(out string? listedMapName)
                            && !string.IsNullOrWhiteSpace(listedMapName))
                        {
                            yield return (listedMapName, propertyName);
                        }
                    }
                }

                if (value is not null)
                {
                    foreach ((string MapName, string Field) child in EnumerateMapReferences(value))
                        yield return child;
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? item in jsonArray)
            {
                if (item is null)
                    continue;

                foreach ((string MapName, string Field) child in EnumerateMapReferences(item))
                    yield return child;
            }
        }
    }

    private static bool IsMapNameField(string propertyName)
        => propertyName.Equals("mapName", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("relativeSongName", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("UsedAsCoach_MapName", StringComparison.OrdinalIgnoreCase);

    private static bool IsMapNameListField(string propertyName)
        => propertyName.Equals("mapNames", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("playlist", StringComparison.OrdinalIgnoreCase);

    private void ResolveLocalContentUrls(JsonNode node, string? assetPlatform)
    {
        string cdnBaseUrl = JduContentUrlResolver.GetCdnBaseUrl(urls);
        ResolveLocalContentUrlsCore(node, cdnBaseUrl, assetPlatform);
    }

    private static void ResolveLocalContentUrlsCore(
        JsonNode node,
        string cdnBaseUrl,
        string? assetPlatform)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (string propertyName in jsonObject.Select(property => property.Key).ToArray())
            {
                JsonNode? value = jsonObject[propertyName];
                if (value is JsonValue jsonValue
                    && jsonValue.TryGetValue(out string? text)
                    && text is not null
                    && text.StartsWith(LocalContentUrlPrefix, StringComparison.Ordinal))
                {
                    string relative = ResolvePlatformPlaceholder(text, assetPlatform)
                        [LocalContentUrlPrefix.Length..]
                        .Replace('\\', '/')
                        .TrimStart('/');
                    jsonObject[propertyName] = $"{cdnBaseUrl}/{CdnContentPaths.JduCatalogAssetsRoot}/{relative}";
                }
                else if (value is not null)
                {
                    ResolveLocalContentUrlsCore(value, cdnBaseUrl, assetPlatform);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            for (int index = 0; index < jsonArray.Count; index++)
            {
                JsonNode? value = jsonArray[index];
                if (value is JsonValue jsonValue
                    && jsonValue.TryGetValue(out string? text)
                    && text is not null
                    && text.StartsWith(LocalContentUrlPrefix, StringComparison.Ordinal))
                {
                    string relative = ResolvePlatformPlaceholder(text, assetPlatform)
                        [LocalContentUrlPrefix.Length..]
                        .Replace('\\', '/')
                        .TrimStart('/');
                    jsonArray[index] = $"{cdnBaseUrl}/{CdnContentPaths.JduCatalogAssetsRoot}/{relative}";
                }
                else if (value is not null)
                {
                    ResolveLocalContentUrlsCore(value, cdnBaseUrl, assetPlatform);
                }
            }
        }
    }

    private static string ResolvePlatformPlaceholder(string url, string? assetPlatform)
    {
        if (!url.Contains(PlatformPlaceholder, StringComparison.Ordinal))
            return url;

        if (string.IsNullOrWhiteSpace(assetPlatform))
        {
            throw new InvalidDataException(
                $"JDU catalog content '{url}' requires a recognized X-SkuId platform.");
        }

        return url.Replace(PlatformPlaceholder, assetPlatform, StringComparison.Ordinal);
    }

    private string GetSafeLocalRelativePath(string url, string sourceFile)
    {
        string relative = url[LocalContentUrlPrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar).Contains(".."))
            throw new InvalidDataException($"JDU catalog '{sourceFile}' contains unsafe local content URL '{url}'.");

        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(paths.JduCatalogAssetsPath));
        string fullPath = Path.GetFullPath(Path.Combine(root, relative));
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"JDU catalog '{sourceFile}' contains unsafe local content URL '{url}'.");

        return relative;
    }

    private static IEnumerable<string> EnumerateStrings(JsonNode node)
    {
        if (node is JsonValue value && value.TryGetValue(out string? text) && text is not null)
        {
            yield return text;
            yield break;
        }

        IEnumerable<JsonNode?> children = node switch
        {
            JsonObject jsonObject => jsonObject.Select(property => property.Value),
            JsonArray jsonArray => jsonArray,
            _ => []
        };

        foreach (JsonNode? child in children)
        {
            if (child is null)
                continue;

            foreach (string childText in EnumerateStrings(child))
                yield return childText;
        }
    }

    private static void MergeObjects(JsonObject destination, JsonObject source)
    {
        foreach ((string propertyName, JsonNode? sourceValue) in source)
        {
            if (destination[propertyName] is JsonObject destinationObject
                && sourceValue is JsonObject sourceObject)
            {
                MergeObjects(destinationObject, sourceObject);
            }
            else if (destination[propertyName] is JsonArray destinationArray
                && sourceValue is JsonArray sourceArray)
            {
                foreach (JsonNode? item in sourceArray)
                    destinationArray.Add(item?.DeepClone());
            }
            else
            {
                destination[propertyName] = sourceValue?.DeepClone();
            }
        }
    }

    private static string NormalizeFamily(string family)
    {
        if (string.IsNullOrWhiteSpace(family))
            throw new ArgumentException("A JDU catalog family is required.", nameof(family));

        return family.Trim().ToLowerInvariant();
    }

    private readonly record struct CatalogFragmentKey(string Family, CatalogScope Scope);

    private readonly record struct CatalogScope(int? GameYear, string? Platform)
    {
        public static CatalogScope Common { get; } = new(null, null);

        public static CatalogScope ForYear(int gameYear) => new(gameYear, null);

        public static CatalogScope ForPlatform(int gameYear, string platform)
            => new(gameYear, JduAssetPlatform.Normalize(platform)
                ?? throw new InvalidDataException($"Unknown JDU asset platform '{platform}'."));

        public static CatalogScope Parse(string directoryName)
        {
            if (directoryName.Equals("common", StringComparison.OrdinalIgnoreCase))
                return Common;

            string[] parts = directoryName.Split('-', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0
                || parts[0].Length != 6
                || !parts[0].StartsWith("jd", StringComparison.OrdinalIgnoreCase)
                || !int.TryParse(parts[0].AsSpan(2), out int gameYear)
                || !JduApplicationClientProfiles.GameYears.Contains(gameYear))
            {
                throw new InvalidDataException(
                    $"Invalid JDU catalog scope '{directoryName}'. Use 'common', 'jdYYYY', or 'jdYYYY-PLATFORM'.");
            }

            return parts.Length == 1
                ? ForYear(gameYear)
                : ForPlatform(gameYear, parts[1]);
        }
    }
}
