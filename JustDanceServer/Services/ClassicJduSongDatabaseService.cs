using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

namespace JustDanceServer.Services;

public interface IClassicJduSongDatabaseService
{
    IReadOnlyDictionary<string, ClassicJduSongDatabaseEntry> CreateDatabase(
        MapVideoCodec? videoPreviewCodec,
        string assetPlatform);
}

public sealed class ClassicJduSongDatabaseService(
    IMapService mapService,
    IOptions<UrlSettings> urlSettings,
    IOptions<ClassicJduDiagnosticsSettings> diagnosticsSettings) : IClassicJduSongDatabaseService
{
    public IReadOnlyDictionary<string, ClassicJduSongDatabaseEntry> CreateDatabase(
        MapVideoCodec? videoPreviewCodec,
        string assetPlatform)
    {
        Dictionary<string, ClassicJduSongDatabaseEntry> database = [with(StringComparer.Ordinal)];

        foreach (ServerMap map in mapService.Maps.Values.Where(map =>
            JduCatalogSelection.IsClassicCompatible(map.Song)))
        {
            JustDanceSongDBEntry source = map.Song;
            if (!string.IsNullOrWhiteSpace(source.MapName))
                database[source.MapName] = ProjectSong(
                    map,
                    urlSettings.Value,
                    videoPreviewCodec,
                    assetPlatform);
        }

        return database;
    }

    private ClassicJduSongDatabaseEntry ProjectSong(
        ServerMap map,
        UrlSettings urlSettings,
        MapVideoCodec? videoPreviewCodec,
        string assetPlatform)
    {
        JustDanceSongDBEntry source = map.Song;
        MapMediaAssets media = map.GetMedia(MapMediaLayouts.JDUnlimited);
        string mapName = source.MapName;
        string[] tags = [.. source.Tags
            .Append("Main")
            .Append("subscribedSong")
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        JduAudioPreviewProjection audioPreview = JduAudioPreviewFactory.Create(source, media, urlSettings);
        JduVideoPreviewProjection videoPreview = videoPreviewCodec.HasValue
            ? JduVideoPreviewFactory.Create(source, media, urlSettings, videoPreviewCodec.Value)
            : new JduVideoPreviewProjection(string.Empty, []);
        Dictionary<string, string> urls = new(audioPreview.Urls, StringComparer.Ordinal);
        foreach ((string logicalUrl, string physicalUrl) in videoPreview.Urls)
            urls[logicalUrl] = physicalUrl;

        IReadOnlyDictionary<string, string> assets =
            JduContentUrlResolver.RebaseAssets(
                JduPlatformAssetProjection.Create(
                    media.GetPlatformAssets(assetPlatform),
                    source.CoachCount,
                    mapName,
                    assetPlatform),
                urlSettings);
        ClassicJduDiagnosticsSettings diagnostics = diagnosticsSettings.Value;
        if (string.Equals(
            mapName,
            diagnostics.AssetOverrideMapName,
            StringComparison.OrdinalIgnoreCase)
            && diagnostics.AssetOverrides.Count > 0)
        {
            assets = diagnostics.AssetOverrides;
        }

        return new ClassicJduSongDatabaseEntry
        {
            MapName = mapName,
            Assets = new Dictionary<string, string>(assets, StringComparer.Ordinal),
            ParentMapName = string.IsNullOrWhiteSpace(source.ParentMapName)
                ? mapName
                : source.ParentMapName,
            Title = source.Title,
            Artist = source.Artist,
            Difficulty = source.Difficulty,
            SweatDifficulty = source.SweatDifficulty,
            MapLength = source.MapLength,
            CoachCount = source.CoachCount is >= 1 and <= 4 ? source.CoachCount : 1,
            Credits = source.Credits,
            Wdf = true,
            AudioPreviewData = audioPreview.Data,
            CustomTypeName = source.DanceVersionLocId.Name ?? string.Empty,
            LyricsColor = NormalizeRgba(source.LyricsColor),
            SongColors = JduSongColorProjection.Create(source.SongColors),
            Tags = tags,
            Urls = urls,
            MapPreviewMpd = videoPreview.Mpd,
            Packages = new Dictionary<string, string>
            {
                ["mapContent"] = $"{mapName}_mapContent"
            },
            OriginalJDVersion = source.OriginalJDVersion > 0 ? source.OriginalJDVersion : 9999,
            DoubleScoringType = TryReadInteger(source.DoubleScoringType)
        };
    }

    private static int? TryReadInteger(object? value)
        => value switch
        {
            null => null,
            int number => number,
            long number when number is >= int.MinValue and <= int.MaxValue => (int)number,
            _ when int.TryParse(value.ToString(), out int number) => number,
            _ => null
        };

    private static string NormalizeRgba(string color)
    {
        string normalized = color.Trim().TrimStart('#').ToUpperInvariant();
        return normalized.Length == 8 && normalized.All(Uri.IsHexDigit)
            ? normalized
            : "FFFFFFFF";
    }
}
