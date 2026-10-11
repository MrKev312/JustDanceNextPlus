using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Catalog;

using Microsoft.Extensions.Options;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Services;

public interface IJduAvatarCatalog : ILoadService
{
    IReadOnlyDictionary<uint, JduAvatarDefinition> Avatars { get; }
}

public sealed class JduAvatarCatalog(
    IOptions<PathSettings> pathOptions,
    IMapService mapService,
    ILogger<JduAvatarCatalog> logger) : IJduAvatarCatalog
{
    private const string ManifestFileName = "Avatars.json";
    private const uint InvalidAvatarId = 0xffff;
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private readonly Lock loadLock = new();
    private readonly PathSettings paths = pathOptions.Value;
    private Task? loadTask;

    public IReadOnlyDictionary<uint, JduAvatarDefinition> Avatars
    {
        get
        {
            EnsureLoaded();
            return field;
        }

        private set;
    } = new ReadOnlyDictionary<uint, JduAvatarDefinition>(new Dictionary<uint, JduAvatarDefinition>());

    public Task LoadData()
    {
        lock (loadLock)
            return loadTask ??= LoadDataCore();
    }

    private async Task LoadDataCore()
    {
        string assetRoot = GetConfiguredAssetRoot();
        string avatarRoot = Path.Combine(assetRoot, "Avatars");
        string soundRoot = Path.Combine(assetRoot, "Sounds");
        if (!Directory.Exists(avatarRoot))
            throw new DirectoryNotFoundException($"JDU avatar catalog '{avatarRoot}' does not exist.");
        if (Directory.EnumerateFiles(avatarRoot, "*", SearchOption.TopDirectoryOnly).FirstOrDefault() is string rootFile)
            throw new InvalidDataException($"JDU avatar root contains ungrouped file '{rootFile}'.");

        Dictionary<string, IReadOnlyList<string>> sounds = LoadSoundGroups(soundRoot);
        Dictionary<uint, JduAvatarDefinition> loaded = [];
        HashSet<string> referencedSounds = [with(StringComparer.OrdinalIgnoreCase)];
        HashSet<string> knownMaps = new(mapService.MapToGuid.Keys, StringComparer.OrdinalIgnoreCase);
        Dictionary<(string MapName, uint Coach), List<uint>> coachAssignments = [];

        string[] groupDirectories = [.. Directory
            .EnumerateDirectories(avatarRoot, "*", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)];
        if (groupDirectories.Length == 0)
            throw new InvalidDataException($"JDU avatar catalog '{avatarRoot}' contains no avatar groups.");
        if (groupDirectories
            .Select(Path.GetFileName)
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1) is { } duplicateGroup)
        {
            throw new InvalidDataException($"Duplicate JDU avatar group name '{duplicateGroup.Key}'.");
        }

        foreach (string groupDirectory in groupDirectories)
        {
            IReadOnlyList<JduAvatarDefinition> group = await LoadGroup(
                groupDirectory,
                sounds,
                referencedSounds,
                knownMaps);
            foreach (JduAvatarDefinition avatar in group)
            {
                if (!loaded.TryAdd(avatar.Id, avatar))
                    throw new InvalidDataException($"Duplicate JDU avatar ID '{avatar.Id}'.");

                if (avatar.MapName is not null && avatar.Coach is uint coach)
                {
                    (string MapName, uint Coach) key = (avatar.MapName.ToUpperInvariant(), coach);
                    if (!coachAssignments.TryGetValue(key, out List<uint>? ids))
                        coachAssignments.Add(key, ids = []);
                    ids.Add(avatar.Id);
                }
            }
        }

        foreach ((string family, _) in sounds)
        {
            if (!referencedSounds.Contains(family))
                throw new InvalidDataException($"JDU sound group '{family}' is not referenced by an avatar.");
        }

        foreach (((string mapName, uint coach), List<uint> ids) in coachAssignments.Where(pair => pair.Value.Count > 1))
        {
            logger.LogWarning(
                "JDU avatars {AvatarIds} share map '{MapName}' coach {Coach}; preserving every stable ID.",
                string.Join(", ", ids),
                mapName,
                coach);
        }

        Avatars = new ReadOnlyDictionary<uint, JduAvatarDefinition>(loaded);
        logger.LogInformation(
            "Loaded {AvatarCount} JDU avatars from {GroupCount} groups with {SoundGroupCount} downloadable sound groups.",
            loaded.Count,
            groupDirectories.Length,
            sounds.Count);
    }

    private async Task<IReadOnlyList<JduAvatarDefinition>> LoadGroup(
        string groupDirectory,
        IReadOnlyDictionary<string, IReadOnlyList<string>> sounds,
        ISet<string> referencedSounds,
        IReadOnlySet<string> knownMaps)
    {
        string[] manifests = [.. Directory.EnumerateFiles(groupDirectory, ManifestFileName, SearchOption.TopDirectoryOnly)];
        if (manifests.Length != 1)
        {
            throw new InvalidDataException(
                $"JDU avatar group '{groupDirectory}' must contain exactly one {ManifestFileName}.");
        }

        string manifestPath = manifests[0];
        await using FileStream stream = File.OpenRead(manifestPath);
        using JsonDocument document = await JsonDocument.ParseAsync(stream);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"JDU avatar manifest '{manifestPath}' must contain an object.");

        string? mapName = GetOptionalString(root, "mapName");
        int? groupJdVersion = GetOptionalInt32(root, "jdVersion");
        if (mapName is not null && !knownMaps.Contains(mapName))
        {
            logger.LogWarning(
                "JDU avatar manifest '{Manifest}' references unknown map '{MapName}'.",
                manifestPath,
                mapName);
        }

        if (!root.TryGetProperty("avatars", out JsonElement entries) || entries.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"JDU avatar manifest '{manifestPath}' requires an 'avatars' object.");

        List<JduAvatarDefinition> result = [];
        HashSet<uint> localIds = [];
        HashSet<string> referencedFiles = [with(StringComparer.OrdinalIgnoreCase), Path.GetFullPath(manifestPath)];
        foreach (JsonProperty property in entries.EnumerateObject())
        {
            if (!uint.TryParse(property.Name, NumberStyles.None, CultureInfo.InvariantCulture, out uint id))
                throw new InvalidDataException($"JDU avatar ID '{property.Name}' in '{manifestPath}' is not a uint.");
            if (id == InvalidAvatarId)
                throw new InvalidDataException($"JDU avatar ID '{id}' in '{manifestPath}' is reserved as invalid.");
            if (!localIds.Add(id))
                throw new InvalidDataException($"Duplicate JDU avatar ID '{id}' in '{manifestPath}'.");

            JduAvatarManifestEntry entry = property.Value.Deserialize<JduAvatarManifestEntry>(SerializerOptions)
                ?? throw new InvalidDataException($"JDU avatar '{id}' in '{manifestPath}' is null.");
            int jdVersion = entry.JdVersion ?? groupJdVersion
                ?? throw new InvalidDataException($"JDU avatar '{id}' in '{manifestPath}' has no jdVersion.");
            if (jdVersion is < 2009 or > 9999)
                throw new InvalidDataException($"JDU avatar '{id}' in '{manifestPath}' has invalid jdVersion '{jdVersion}'.");
            if (mapName is not null && entry.Coach is null)
                throw new InvalidDataException($"Map-linked JDU avatar '{id}' in '{manifestPath}' requires a coach.");
            if (mapName is null && entry.Coach is not null)
                throw new InvalidDataException($"Standalone JDU avatar '{id}' in '{manifestPath}' cannot declare a coach.");
            if (entry.Price is < 0)
                throw new InvalidDataException($"JDU avatar '{id}' in '{manifestPath}' has a negative price.");

            string imagePath = GetSafeExistingFile(groupDirectory, entry.Image, manifestPath);
            referencedFiles.Add(imagePath);
            string catalogImagePath = GetCatalogRelativePath(imagePath);
            IReadOnlyList<string> soundPaths = [];
            string? soundFamily = NormalizeOptional(entry.Sound);
            if (soundFamily is not null && !soundFamily.StartsWith("AVTR_", StringComparison.Ordinal))
                throw new InvalidDataException($"JDU avatar '{id}' in '{manifestPath}' has non-AVTR sound family '{soundFamily}'.");
            if (soundFamily is not null && sounds.TryGetValue(soundFamily, out IReadOnlyList<string>? files))
            {
                string storedFamily = sounds.Keys.First(family =>
                    family.Equals(soundFamily, StringComparison.OrdinalIgnoreCase));
                if (!storedFamily.Equals(soundFamily, StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"JDU avatar '{id}' in '{manifestPath}' must reference sound family using exact case '{storedFamily}'.");
                }

                referencedSounds.Add(soundFamily);
                soundPaths = [.. files.Select(GetCatalogRelativePath)];
            }

            result.Add(new(
                id,
                jdVersion,
                mapName,
                entry.Coach,
                catalogImagePath,
                soundFamily,
                soundPaths,
                entry.Status ?? JduCustomizableItemStatus.Unlocked,
                entry.UnlockType ?? JduCustomizableItemUnlockType.UnlockedByDefault,
                entry.Price ?? 0,
                NormalizeOptional(entry.RelatedSong),
                NormalizeOptional(entry.RelatedQuest),
                NormalizeOptional(entry.RelatedWdfBoss)));
        }

        if (result.Count == 0)
            throw new InvalidDataException($"JDU avatar manifest '{manifestPath}' contains no avatars.");

        string[] orphanedFiles = [.. Directory
            .EnumerateFiles(groupDirectory, "*", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .Where(file => !referencedFiles.Contains(file))];
        if (orphanedFiles.Length != 0)
        {
            throw new InvalidDataException(
                $"JDU avatar group '{groupDirectory}' contains unreferenced file '{orphanedFiles[0]}'.");
        }

        return result;
    }

    private static Dictionary<string, IReadOnlyList<string>> LoadSoundGroups(string soundRoot)
    {
        Dictionary<string, IReadOnlyList<string>> sounds = [with(StringComparer.OrdinalIgnoreCase)];
        if (!Directory.Exists(soundRoot))
            return sounds;
        if (Directory.EnumerateFiles(soundRoot, "*", SearchOption.TopDirectoryOnly).FirstOrDefault() is string rootFile)
            throw new InvalidDataException($"JDU sound root contains ungrouped file '{rootFile}'.");

        foreach (string directory in Directory.EnumerateDirectories(soundRoot, "*", SearchOption.TopDirectoryOnly))
        {
            string family = Path.GetFileName(directory);
            if (!family.StartsWith("AVTR_", StringComparison.Ordinal))
                throw new InvalidDataException($"JDU sound group '{family}' must use its exact AVTR_ family name.");
            string[] files = [.. Directory
                .EnumerateFiles(directory, "*.ogg", SearchOption.TopDirectoryOnly)
                .Order(NaturalFileNameComparer.Instance)];
            if (files.Length == 0)
                throw new InvalidDataException($"JDU sound group '{directory}' contains no .ogg files.");
            if (!sounds.TryAdd(family, files))
                throw new InvalidDataException($"Duplicate JDU sound group '{family}'.");
            string? unexpectedFile = Directory
                .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .FirstOrDefault(file => !files.Contains(file, StringComparer.OrdinalIgnoreCase));
            if (unexpectedFile is not null)
                throw new InvalidDataException($"JDU sound group '{family}' contains unexpected file '{unexpectedFile}'.");
        }

        return sounds;
    }

    private string GetConfiguredAssetRoot()
    {
        if (string.IsNullOrWhiteSpace(paths.JduCatalogAssetsPath))
            throw new InvalidDataException("Paths:JduCatalogAssetsPath is required for the JDU avatar catalog.");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(paths.JduCatalogAssetsPath));
    }

    private string GetCatalogRelativePath(string fullPath)
    {
        string relative = Path.GetRelativePath(GetConfiguredAssetRoot(), fullPath);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new InvalidDataException($"JDU avatar asset '{fullPath}' escapes the catalog asset root.");
        return relative.Replace('\\', '/');
    }

    private static string GetSafeExistingFile(string root, string relativePath, string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException($"JDU avatar manifest '{manifestPath}' has invalid image path '{relativePath}'.");

        string normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
        if (!fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"JDU avatar manifest '{manifestPath}' has escaping image path '{relativePath}'.");
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"JDU avatar manifest '{manifestPath}' references missing image '{relativePath}'.", fullPath);
        return fullPath;
    }

    private static string? GetOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value))
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException($"JDU avatar manifest property '{propertyName}' must be a string.");
        return NormalizeOptional(value.GetString());
    }

    private static int? GetOptionalInt32(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value))
            return null;
        if (!value.TryGetInt32(out int result))
            throw new InvalidDataException($"JDU avatar manifest property '{propertyName}' must be an integer.");
        return result;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void EnsureLoaded()
    {
        Task task = LoadData();
        if (!task.IsCompletedSuccessfully)
            task.GetAwaiter().GetResult();
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
