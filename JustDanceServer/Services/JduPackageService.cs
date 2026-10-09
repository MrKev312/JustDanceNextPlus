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
    JduPackageArchiveMetadata? GetArchiveMetadata(string archiveMd5, string assetPlatform);
    JduPackageArchive? GetArchive(string archiveMd5, string assetPlatform);
}

public sealed class JduPackageService(
    IOptions<PathSettings> pathSettings,
    IOptions<UrlSettings> urlSettings,
    IFileSystem fileSystem,
    ILogger<JduPackageService> logger) : IJduPackageService
{
    private readonly ConcurrentDictionary<string, Lazy<PackageIndex>> indexes = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, JduPackageEntry> CreateCatalog(string? skuId)
    {
        string? assetPlatform = JduAssetPlatform.FromSkuId(skuId);
        if (assetPlatform is null)
        {
            logger.LogWarning(
                "Cannot build the JDU package catalog because X-SkuId '{SkuId}' does not identify a supported platform.",
                skuId);
            return new Dictionary<string, JduPackageEntry>();
        }

        Dictionary<string, JduPackageEntry> catalog = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (PackageSource source in GetIndex(assetPlatform).Sources)
        {
            byte[] hash = Convert.FromHexString(source.Md5);
            string packageId = source.PackageId;
            string archiveFileName = Path.GetFileName(source.ArchivePath);

            catalog[packageId] = new JduPackageEntry
            {
                Url = $"{GetBaseUrl()}/packages/v1/maps/{assetPlatform}/{Uri.EscapeDataString(archiveFileName)}",
                Md5 = source.Md5,
                Version = CreateVersion(hash),
                StorageType = 0
            };
        }

        logger.LogInformation(
            "Built the JDU package catalog for X-SkuId '{SkuId}' ({AssetPlatform}) with {PackageCount} package(s).",
            skuId,
            assetPlatform,
            catalog.Count);

        return catalog;
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
            PackageSource[] sources = [.. FindPackageSources(value)];
            return new PackageIndex(sources, sources.GroupBy(source => source.Md5, StringComparer.OrdinalIgnoreCase)
                .ToFrozenDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase));
        }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    private IEnumerable<PackageSource> FindPackageSources(string assetPlatform)
    {
        string mapsPath = pathSettings.Value.MapsPath;
        if (!fileSystem.DirectoryExists(mapsPath))
            yield break;

        foreach (string mapDirectory in fileSystem.GetDirectories(mapsPath))
        {
            string mapName = Path.GetFileName(mapDirectory);
			string packageDirectory = Path.Combine(mapDirectory, "JDUnlimited", "Assets", assetPlatform, "MainScene");
            if (!fileSystem.DirectoryExists(packageDirectory))
                continue;

            string[] archives = [.. fileSystem.GetFiles(packageDirectory, "*.zip")];
            if (archives.Length != 1)
                continue;

            string archiveMd5 = Path.GetFileNameWithoutExtension(archives[0]);
            if (!IsMd5(archiveMd5))
                continue;

            yield return new PackageSource($"{mapName}_mapContent", archives[0], archiveMd5.ToLowerInvariant());
        }
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

    private sealed record PackageSource(string PackageId, string ArchivePath, string Md5);
    private sealed record PackageIndex(PackageSource[] Sources, FrozenDictionary<string, PackageSource> ByMd5);
}
