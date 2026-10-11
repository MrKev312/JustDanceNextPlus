using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Catalog;

using Microsoft.Extensions.Options;

using System.Collections.ObjectModel;

namespace JustDanceServer.Services;

public interface IJduCustomizableItemCatalog : ILoadService
{
    IReadOnlyDictionary<uint, JduSkinDefinition> Skins { get; }
    IReadOnlyDictionary<uint, JduPortraitBorderDefinition> PortraitBorders { get; }
}

public sealed class JduCustomizableItemCatalog(
    IOptions<PathSettings> pathOptions,
    ILogger<JduCustomizableItemCatalog> logger) : IJduCustomizableItemCatalog
{
    private const uint InvalidItemId = 0xffff;
    private readonly Lock loadLock = new();
    private readonly JduCatalogAssetDirectory assets = new(pathOptions.Value);
    private Task? loadTask;

    public IReadOnlyDictionary<uint, JduSkinDefinition> Skins
    {
        get
        {
            EnsureLoaded();
            return field;
        }

        private set;
    } = Empty<JduSkinDefinition>();
    public IReadOnlyDictionary<uint, JduPortraitBorderDefinition> PortraitBorders
    {
        get
        {
            EnsureLoaded();
            return field;
        }

        private set;
    } = Empty<JduPortraitBorderDefinition>();

    public Task LoadData()
    {
        lock (loadLock)
            return loadTask ??= LoadDataCore();
    }

    private async Task LoadDataCore()
    {
        Dictionary<uint, JduSkinDefinition> loadedSkins = [];
        foreach (string group in JduCatalogAssetDirectory.GetGroups(assets.GetFamilyRoot("Skins")))
        {
            JduSkinManifest manifest = await JduCatalogAssetDirectory.LoadManifest<JduSkinManifest>(group, "Skin.json");
            ValidateItemId(manifest.Id, "skin", group);
            if (manifest.JdVersion is < 2009 or > 9999)
                throw new InvalidDataException($"JDU skin '{manifest.Id}' has invalid jdVersion '{manifest.JdVersion}'.");
            if (manifest.Price is < 0)
                throw new InvalidDataException($"JDU skin '{manifest.Id}' has a negative price.");

            string image = JduCatalogAssetDirectory.GetRequiredFile(group, "Skin.png");
            JduCatalogAssetDirectory.RejectUnexpectedFiles(group, [Path.Combine(group, "Skin.json"), image]);
            if (!loadedSkins.TryAdd(manifest.Id, new(
                manifest.Id,
                manifest.JdVersion,
                assets.GetRelativePath(image),
                manifest.Status ?? JduCustomizableItemStatus.Unlocked,
                manifest.UnlockType ?? JduCustomizableItemUnlockType.UnlockedByDefault,
                manifest.Price ?? 0)))
            {
                throw new InvalidDataException($"Duplicate JDU skin ID '{manifest.Id}'.");
            }
        }

        Dictionary<uint, JduPortraitBorderDefinition> loadedBorders = [];
        foreach (string group in JduCatalogAssetDirectory.GetGroups(assets.GetFamilyRoot("PortraitBorders")))
        {
            JduPortraitBorderManifest manifest = await JduCatalogAssetDirectory.LoadManifest<JduPortraitBorderManifest>(group, "Border.json");
            ValidateItemId(manifest.Id, "portrait border", group);
            if (manifest.OriginalLockStatus is not (0 or 1) || manifest.Visibility is not (0 or 1))
                throw new InvalidDataException($"JDU portrait border '{manifest.Id}' has invalid lock or visibility state.");

            string background = JduCatalogAssetDirectory.GetRequiredFile(group, "Background.png");
            string? foreground = JduCatalogAssetDirectory.GetOptionalFile(group, "Foreground.png");
            JduCatalogAssetDirectory.RejectUnexpectedFiles(
                group,
                [Path.Combine(group, "Border.json"), background, .. foreground is null ? [] : new[] { foreground }]);
            if (!loadedBorders.TryAdd(manifest.Id, new(
                manifest.Id,
                assets.GetRelativePath(background),
                foreground is null ? null : assets.GetRelativePath(foreground),
                manifest.OriginalLockStatus,
                manifest.Visibility)))
            {
                throw new InvalidDataException($"Duplicate JDU portrait border ID '{manifest.Id}'.");
            }
        }

        Skins = new ReadOnlyDictionary<uint, JduSkinDefinition>(loadedSkins);
        PortraitBorders = new ReadOnlyDictionary<uint, JduPortraitBorderDefinition>(loadedBorders);
        logger.LogInformation(
            "Loaded {SkinCount} JDU skins and {BorderCount} portrait borders.",
            loadedSkins.Count,
            loadedBorders.Count);
    }

    private static void ValidateItemId(uint id, string kind, string group)
    {
        if (id == InvalidItemId)
            throw new InvalidDataException($"JDU {kind} ID '{id}' in '{group}' is reserved as invalid.");
    }

    private void EnsureLoaded()
    {
        Task task = LoadData();
        if (!task.IsCompletedSuccessfully)
            task.GetAwaiter().GetResult();
    }

    private static IReadOnlyDictionary<uint, T> Empty<T>()
        => new ReadOnlyDictionary<uint, T>(new Dictionary<uint, T>());
}
