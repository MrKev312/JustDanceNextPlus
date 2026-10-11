using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Catalog;

using Microsoft.Extensions.Options;

using System.Collections.ObjectModel;

namespace JustDanceServer.Services;

public interface IJduQuestCatalog : ILoadService
{
    IReadOnlyDictionary<uint, JduQuestDefinition> Quests { get; }
}

public sealed class JduQuestCatalog(
    IOptions<PathSettings> pathOptions,
    IMapService mapService,
    ILogger<JduQuestCatalog> logger) : IJduQuestCatalog
{
    private readonly Lock loadLock = new();
    private readonly JduCatalogAssetDirectory assets = new(pathOptions.Value);
    private Task? loadTask;

    public IReadOnlyDictionary<uint, JduQuestDefinition> Quests
    {
        get
        {
            EnsureLoaded();
            return field;
        }

        private set;
    } = new ReadOnlyDictionary<uint, JduQuestDefinition>(new Dictionary<uint, JduQuestDefinition>());

    public Task LoadData()
    {
        lock (loadLock)
            return loadTask ??= LoadDataCore();
    }

    private async Task LoadDataCore()
    {
        HashSet<string> knownMaps = new(mapService.MapToGuid.Keys, StringComparer.OrdinalIgnoreCase);
        Dictionary<uint, JduQuestDefinition> loaded = [];
        foreach (string group in JduCatalogAssetDirectory.GetGroups(assets.GetFamilyRoot("Quests")))
        {
            JduQuestManifest manifest = await JduCatalogAssetDirectory.LoadManifest<JduQuestManifest>(group, "Quest.json");
            if (string.IsNullOrWhiteSpace(manifest.Title) || manifest.Maps.Length == 0)
                throw new InvalidDataException($"JDU quest '{manifest.Id}' in '{group}' requires a title and maps.");
            if (manifest.Maps.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException($"JDU quest '{manifest.Id}' in '{group}' contains an empty map name.");
            if (manifest.Maps.Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Maps.Length)
                throw new InvalidDataException($"JDU quest '{manifest.Id}' in '{group}' contains a duplicate map.");
            foreach (string mapName in manifest.Maps.Where(mapName => !knownMaps.Contains(mapName)))
                logger.LogWarning("JDU quest {QuestId} references unknown map '{MapName}'.", manifest.Id, mapName);

            string phone = JduCatalogAssetDirectory.GetRequiredFile(group, "Phone.png");
            Dictionary<string, JduQuestPlatformAssets> platformAssets = [with(StringComparer.OrdinalIgnoreCase)];
            List<string> expectedFiles = [Path.Combine(group, "Quest.json"), phone];
            foreach (string platform in JduAssetPlatform.SupportedPlatforms)
            {
                string platformRoot = Path.Combine(group, "Assets", platform);
                string cover = JduCatalogAssetDirectory.GetRequiredFile(platformRoot, "Cover.ckd");
                string logo = JduCatalogAssetDirectory.GetRequiredFile(platformRoot, "Logo.ckd");
                string shadedLogo = JduCatalogAssetDirectory.GetRequiredFile(platformRoot, "ShadedLogo.ckd");
                expectedFiles.AddRange([cover, logo, shadedLogo]);
                platformAssets.Add(platform, new(
                    assets.GetRelativePath(cover),
                    assets.GetRelativePath(logo),
                    assets.GetRelativePath(shadedLogo)));
            }

            JduCatalogAssetDirectory.RejectUnexpectedFiles(group, expectedFiles);

            if (!loaded.TryAdd(manifest.Id, new(
                manifest.Id,
                manifest.Title,
                manifest.Maps,
                manifest.Locked,
                assets.GetRelativePath(phone),
                new ReadOnlyDictionary<string, JduQuestPlatformAssets>(platformAssets))))
            {
                throw new InvalidDataException($"Duplicate JDU quest ID '{manifest.Id}'.");
            }
        }

        Quests = new ReadOnlyDictionary<uint, JduQuestDefinition>(loaded);
        logger.LogInformation("Loaded {QuestCount} self-contained JDU quests.", loaded.Count);
    }

    private void EnsureLoaded()
    {
        Task task = LoadData();
        if (!task.IsCompletedSuccessfully)
            task.GetAwaiter().GetResult();
    }
}
