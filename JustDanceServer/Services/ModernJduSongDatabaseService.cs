using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceServer.Services;

public interface IModernJduSongDatabaseService
{
    IReadOnlyDictionary<string, JduSongDatabaseEntry> CreateDatabase(string assetPlatform);
    byte[] SerializeDatabase(string assetPlatform);
    byte[] SerializeLocalisation();
}

public sealed class ModernJduSongDatabaseService(
    IMapService mapService,
    JsonSettingsService jsonSettingsService,
    IOptions<UrlSettings> urlSettings,
    ILocalizedStringService text) : IModernJduSongDatabaseService
{
    public IReadOnlyDictionary<string, JduSongDatabaseEntry> CreateDatabase(string assetPlatform)
    {
        Dictionary<string, JduSongDatabaseEntry> database = [with(StringComparer.Ordinal)];

        foreach (ServerMap map in mapService.Maps.Values)
        {
            JustDanceSongDBEntry source = map.Song;
            if (string.IsNullOrWhiteSpace(source.MapName))
                continue;

            database[source.MapName] = ProjectSong(map, urlSettings.Value, assetPlatform, text);
        }

        return database;
    }

    public byte[] SerializeDatabase(string assetPlatform)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            CreateDatabase(assetPlatform),
            jsonSettingsService.PrettyPascalFormat);
    }

    public byte[] SerializeLocalisation()
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            text.GetJduLocalisation(),
            jsonSettingsService.PrettyPascalFormat);
    }

    private static JduSongDatabaseEntry ProjectSong(
        ServerMap map,
        UrlSettings urlSettings,
        string assetPlatform,
        ILocalizedStringService text)
    {
        JustDanceSongDBEntry source = map.Song;
        MapMediaAssets media = map.GetMedia(MapMediaLayouts.JDUnlimited);
        string[] tags = [.. source.Tags
            .Append("Main")
            .Append("subscribedSong")
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        string mapName = source.MapName;
        JduAudioPreviewProjection audioPreview = JduAudioPreviewFactory.Create(source, media, urlSettings);
        if (!JduAssetPlatform.TryParse(assetPlatform, out JduPlatform platform))
            throw new ArgumentOutOfRangeException(
                nameof(assetPlatform),
                assetPlatform,
                "The JDU asset platform is not supported.");
        JduVideoPreviewProjection videoPreview = JduVideoPreviewFactory.Create(
            source,
            media,
            urlSettings,
            JduVideoCapabilities.GetCodec(ModernJduConstants.FirstGameYear, platform));
        Dictionary<string, string> urls = new(audioPreview.Urls, StringComparer.Ordinal);
        foreach ((string logicalUrl, string physicalUrl) in videoPreview.Urls)
            urls[logicalUrl] = physicalUrl;

        return new JduSongDatabaseEntry
        {
            Artist = source.Artist,
            Assets = JduContentUrlResolver.RebaseAssets(
                JduPlatformAssetProjection.Create(
                    media.GetPlatformAssets(assetPlatform),
                    source.CoachCount,
                    source.MapName,
                    assetPlatform),
                urlSettings),
            AudioPreviewData = audioPreview.Data,
            CoachCount = source.CoachCount is >= 1 and <= 4 ? source.CoachCount : 1,
            Credits = source.Credits,
            CustomTypeName = source.DanceVersionLocId.Name ?? string.Empty,
            CustomTypeNameId = text.GetJduAlias(source.DanceVersionLocId.ID),
            Difficulty = source.Difficulty,
            LyricsColor = NormalizeRgba(source.LyricsColor),
            MapLength = source.MapLength,
            MapName = mapName,
            MapPreviewMpd = videoPreview.Mpd,
            OriginalJDVersion = NormalizeVersion(source.OriginalJDVersion),
            Packages = new Dictionary<string, string>
            {
                ["mapContent"] = $"{mapName}_mapContent"
            },
            ParentMapName = string.IsNullOrWhiteSpace(source.ParentMapName)
                ? mapName
                : source.ParentMapName,
            SweatDifficulty = source.SweatDifficulty,
            SongColors = JduSongColorProjection.Create(source.SongColors),
            Tags = tags,
            Title = source.Title,
            Urls = urls,
            Wdf = true
        };
    }

    private static int NormalizeVersion(int version)
    {
        return version > 0 ? version : 9999;
    }

    private static string NormalizeRgba(string color)
    {
        string normalized = color.Trim().TrimStart('#').ToUpperInvariant();
        return normalized.Length == 8 && normalized.All(Uri.IsHexDigit)
            ? normalized
            : "FFFFFFFF";
    }
}
