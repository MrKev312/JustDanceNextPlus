using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace JustDanceServer.Services;

public interface IJduPackageService
{
    IReadOnlyDictionary<string, JduPackageEntry> CreateCatalog(string? skuId);
    JduPackageSelection? SelectPackage(string mapName, JduMediaClientProfile client);
    JduPackageArchiveMetadata? GetArchiveMetadata(string archiveMd5, string assetPlatform);
    JduPackageArchive? GetArchive(string archiveMd5, string assetPlatform);
}

public sealed record JduPackageSelection(MapVideoCodec VideoCodec, JduPackageEntry Package);

public sealed class JduPackageService(
    IMapService mapService,
    IOptions<UrlSettings> urlSettings,
    IFileSystem fileSystem,
    ILogger<JduPackageService> logger) : IJduPackageService
{
    private readonly ConcurrentDictionary<string, Lazy<PackageIndex>> indexes = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, JduPackageEntry> CreateCatalog(string? skuId)
    {
        if (!JduMediaClientProfiles.TryResolve(skuId, out JduMediaClientProfile? client))
        {
            logger.LogWarning(
                "Cannot build the JDU package catalog because X-SkuId '{SkuId}' does not identify a supported platform.",
                skuId);
            return new Dictionary<string, JduPackageEntry>();
        }

        string assetPlatform = client.AssetPlatform;
        Dictionary<string, JduPackageEntry> catalog = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (PackageVariants variants in GetIndex(assetPlatform).ByMapName.Values)
        {
            JduPackageSelection selection = SelectPackage(variants, client);
            catalog[variants.Vp8.PackageId] = selection.Package;
        }

        logger.LogInformation(
            "Built the JDU package catalog for X-SkuId '{SkuId}' ({AssetPlatform}) with {PackageCount} package(s).",
            skuId,
            assetPlatform,
            catalog.Count);

        return catalog;
    }

    public JduPackageSelection? SelectPackage(string mapName, JduMediaClientProfile client)
        => GetIndex(client.AssetPlatform).ByMapName.TryGetValue(mapName, out PackageVariants? variants)
            ? SelectPackage(variants, client)
            : null;

    private JduPackageSelection SelectPackage(PackageVariants variants, JduMediaClientProfile client)
    {
        PackageSource source = client.VideoCodec == MapVideoCodec.Vp9 && variants.Vp9 is not null
            ? variants.Vp9
            : variants.Vp8;
        string archiveFileName = Path.GetFileName(source.ArchivePath);
        JduPackageEntry package = new()
        {
            Url = $"{GetBaseUrl()}/packages/v1/maps/{client.AssetPlatform}/{Uri.EscapeDataString(archiveFileName)}",
            Md5 = source.Md5,
            Version = CreateVersion(Convert.FromHexString(source.Md5)),
            StorageType = 0
        };
        return new JduPackageSelection(source.VideoCodec, package);
    }

    public JduPackageArchive? GetArchive(string archiveMd5, string assetPlatform)
    {
        PackageSource? source = FindArchive(archiveMd5, assetPlatform);

        return source is null
            ? null
            : new JduPackageArchive(source.PackageId, Path.GetFileName(source.ArchivePath), fileSystem.OpenRead(source.ArchivePath));
    }

    public JduPackageArchiveMetadata? GetArchiveMetadata(string archiveMd5, string assetPlatform)
    {
        PackageSource? source = FindArchive(archiveMd5, assetPlatform);
        return source is null
            ? null
            : new JduPackageArchiveMetadata(
                source.PackageId,
                Path.GetFileName(source.ArchivePath),
                fileSystem.GetFileLength(source.ArchivePath));
    }

    private PackageSource? FindArchive(string archiveMd5, string assetPlatform)
    {
        string? normalizedPlatform = JduAssetPlatform.Normalize(assetPlatform);
        return normalizedPlatform is null
            ? null
            : GetIndex(normalizedPlatform).ByMd5.GetValueOrDefault(archiveMd5);
    }

    private PackageIndex GetIndex(string platform)
        => indexes.GetOrAdd(platform, value => new Lazy<PackageIndex>(() =>
        {
            PackageVariants[] variants = [.. FindPackageSources(value)];
            IEnumerable<PackageSource> sources = variants.SelectMany(variant => variant.Vp9 is { } vp9
                ? new[] { variant.Vp8, vp9 }
                : new[] { variant.Vp8 });
            return new PackageIndex(
                variants.ToFrozenDictionary(variant => variant.MapName, StringComparer.OrdinalIgnoreCase),
                sources.GroupBy(source => source.Md5, StringComparer.OrdinalIgnoreCase)
                    .ToFrozenDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase));
        }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    private IEnumerable<PackageVariants> FindPackageSources(string assetPlatform)
    {
        foreach (ServerMap map in mapService.Maps.Values)
        {
            string mapName = map.Song.MapName;
            string assetsDirectory = Path.Combine(map.FolderPath, "JDUnlimited", "Assets", assetPlatform);
            PackageSource? vp8 = FindPackageSource(assetsDirectory, mapName, "MainScene", MapVideoCodec.Vp8);
            if (vp8 is null)
                continue;

            PackageSource? vp9 = FindPackageSource(assetsDirectory, mapName, "MainSceneVP9", MapVideoCodec.Vp9);
            yield return new PackageVariants(mapName, vp8, vp9);
        }
    }

    private PackageSource? FindPackageSource(string assetsDirectory, string mapName, string role, MapVideoCodec codec)
    {
        string packageDirectory = Path.Combine(assetsDirectory, role);
        if (!fileSystem.DirectoryExists(packageDirectory))
            return null;

        string[] archives = fileSystem.GetFiles(packageDirectory, "*.zip");
        if (archives.Length != 1)
            return null;

        string archiveMd5 = Path.GetFileNameWithoutExtension(archives[0]);
        return IsMd5(archiveMd5)
            ? new PackageSource($"{mapName}_mapContent", archives[0], archiveMd5.ToLowerInvariant(), codec)
            : null;
    }

    private string GetBaseUrl()
        => JduContentUrlResolver.GetCdnBaseUrl(urlSettings.Value);

    private static int CreateVersion(ReadOnlySpan<byte> hash)
    {
        int version = (int)(BinaryPrimitives.ReadUInt32BigEndian(hash) & 0x7fff_ffff);
        return version == 0 ? 1 : version;
    }

    private static bool IsMd5(string value)
        => value.Length == 32 && value.All(Uri.IsHexDigit);

    private sealed record PackageSource(string PackageId, string ArchivePath, string Md5, MapVideoCodec VideoCodec);
    private sealed record PackageVariants(string MapName, PackageSource Vp8, PackageSource? Vp9);
    private sealed record PackageIndex(
        FrozenDictionary<string, PackageVariants> ByMapName,
        FrozenDictionary<string, PackageSource> ByMd5);
}
