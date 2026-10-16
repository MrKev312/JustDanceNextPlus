using JustDanceServer.Configuration;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Options;

namespace JustDanceServer.Services;

public interface IMapMediaLoader
{
    MapMediaAssets LoadMedia(string mapFolder, MapMediaLayout layout);
    MapVideoSet LoadVideoSet(string mapFolder, string relativeFolder, string codec);
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadPlatformAssets(
        string mapFolder,
        MapMediaLayout layout);
}

public sealed class MapMediaLoader(
    IOptions<UrlSettings> urlOptions,
    IFileSystem fileSystem,
    IVideoFileInspector videoFileInspector) : IMapMediaLoader
{
    private readonly UrlSettings urlSettings = urlOptions.Value;

    public MapMediaAssets LoadMedia(string mapFolder, MapMediaLayout layout)
    {
        IReadOnlyDictionary<string, MapVideoSet> previewVideos = layout.PreviewVideoCodecs.ToDictionary(
            codec => codec,
            codec => LoadVideoSet(mapFolder, layout.GetPreviewVideoFolder(codec), codec),
            StringComparer.OrdinalIgnoreCase);

        return new MapMediaAssets(
            layout,
            GetAudioUrl(mapFolder, layout.PreviewAudio),
            GetAudioUrl(mapFolder, layout.FullAudio),
            previewVideos,
            LoadPlatformAssets(mapFolder, layout));
    }

    public MapVideoSet LoadVideoSet(string mapFolder, string relativeFolder, string codec)
    {
        string physicalFolder = relativeFolder.Replace('/', Path.DirectorySeparatorChar);
        WebmData[] qualities = videoFileInspector.GetVideoData(Path.Combine(mapFolder, physicalFolder));
        IReadOnlyList<string> urls = GetVideoUrls(qualities, mapFolder, relativeFolder);

        return new MapVideoSet(
            MapVideoCodecExtensions.ParseCodecName(codec),
            [.. qualities.Select((data, index) => new MapVideoQuality(data, urls[index]))]);
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadPlatformAssets(
        string mapFolder,
        MapMediaLayout layout)
    {
        string assetsFolder = Path.Combine(mapFolder, layout.FolderName, "Assets");
        if (!fileSystem.DirectoryExists(assetsFolder))
            throw new DirectoryNotFoundException($"Missing {layout.FolderName} assets folder {assetsFolder}.");

        Dictionary<string, IReadOnlyDictionary<string, string>> platforms =
            [with(StringComparer.OrdinalIgnoreCase)];
        foreach (string platformFolder in fileSystem.GetDirectories(assetsFolder))
        {
            string platform = Path.GetFileName(platformFolder);
            Dictionary<string, string> roles = [with(StringComparer.OrdinalIgnoreCase)];
            foreach (string roleFolder in fileSystem.GetDirectories(platformFolder))
            {
                string role = Path.GetFileName(roleFolder);
                string[] files = fileSystem.GetFiles(roleFolder);
                if (files.Length != 1)
                {
                    throw new InvalidDataException(
                        $"{layout.FolderName} {platform} asset role '{role}' in {roleFolder} must contain exactly one file; found {files.Length}.");
                }

                string relativeFolder = $"{layout.FolderName}/Assets/{platform}/{role}";
                roles[role] = BuildAssetUrl(mapFolder, relativeFolder, files[0]);
            }

            if (roles.Count == 0)
                throw new InvalidDataException($"{layout.FolderName} {platform} has no platform asset roles.");
            platforms[platform] = roles;
        }

        if (platforms.Count == 0)
            throw new InvalidDataException($"{layout.FolderName} has no platform assets.");
        return platforms;
    }

    internal IReadOnlyList<string> GetVideoUrls(
        IReadOnlyList<WebmData> videoData,
        string mapFolder,
        string relativeFolder)
    {
        if (videoData.Count == 0)
        {
            throw new InvalidOperationException(
                "Expected at least one video.");
        }

        string normalizedFolder = relativeFolder.Trim('/').Replace('\\', '/');
        string baseUrl = $"{CdnBaseUrl}/maps/{Path.GetFileName(mapFolder)}/{normalizedFolder}";
        return [.. videoData.Select(video => $"{baseUrl}/{video.FileName}.webm")];
    }

    private string GetAudioUrl(string mapFolder, MapAudioLayout audioLayout)
    {
        string relativeFolder = audioLayout.RelativeFolder.Replace('/', Path.DirectorySeparatorChar);
        string assetFolder = Path.Combine(mapFolder, relativeFolder);
        if (!fileSystem.DirectoryExists(assetFolder))
            throw new DirectoryNotFoundException($"Missing audio folder {assetFolder}.");

        string? file = fileSystem.GetFiles(assetFolder)
            .Where(path => Path.GetExtension(path).Equals(audioLayout.FileExtension, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? throw new FileNotFoundException($"Missing {audioLayout.FileExtension} audio file in {assetFolder}.");
        return BuildAssetUrl(mapFolder, relativeFolder.Replace('\\', '/'), file);
    }

    private string BuildAssetUrl(string mapFolder, string urlFolder, string file)
        => $"{CdnBaseUrl}/maps/{Path.GetFileName(mapFolder)}/{urlFolder}/{Path.GetFileName(file)}";

    private string CdnBaseUrl
    {
        get
        {
            string trimmedUrl = urlSettings.CDNUrl.Trim().TrimEnd('/');
            return trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? trimmedUrl
                : $"https://{trimmedUrl}";
        }
    }
}
