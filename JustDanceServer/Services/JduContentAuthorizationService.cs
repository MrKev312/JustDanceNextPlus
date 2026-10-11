using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

namespace JustDanceServer.Services;

public interface IJduContentAuthorizationService
{
    JduContentAuthorizationEntry? Create(string mapName, JduMediaClientProfile client);
}

public sealed class JduContentAuthorizationService(
    IMapService mapService,
    IJduPackageService packageService,
    IOptions<UrlSettings> urlOptions) : IJduContentAuthorizationService
{
    public JduContentAuthorizationEntry? Create(string mapName, JduMediaClientProfile client)
    {
        ServerMap? map = mapService.GetMap(mapName);
        if (map is null)
            return null;

        JustDanceSongDBEntry song = map.Song;
        string canonicalMapName = song.MapName;
        JduPackageSelection? selection = packageService.SelectPackage(canonicalMapName, client);
        if (selection is null)
            return null;

        MapMediaAssets media = map.GetMedia(MapMediaLayouts.JDUnlimited);
        MapVideoSet videos = map.GetFullVideos(selection.VideoCodec.ToCodecName());
        if (!JduVideoCapabilities.Supports1080p(client.Platform))
            videos = UseSecondHighestForUltra(videos);
        Dictionary<string, string> urls = new(StringComparer.Ordinal)
        {
            [$"jmcs://jd-contents/{canonicalMapName}/{canonicalMapName}.ogg"] =
                JduContentUrlResolver.RebaseToCdn(media.FullAudioUrl, urlOptions.Value)
        };

        for (int index = 0; index < videos.Qualities.Count; index++)
        {
            string authorizedUrl = JduContentUrlResolver.RebaseToCdn(videos.Qualities[index].Url, urlOptions.Value);
            string logicalBase = $"jmcs://jd-contents/{canonicalMapName}/{canonicalMapName}_{MapVideoSet.QualityNames[index]}";
            urls[$"{logicalBase}.webm"] = authorizedUrl;
            urls[$"{logicalBase}.hd.webm"] = authorizedUrl;
        }

        return new JduContentAuthorizationEntry { Urls = urls };
    }

    private static MapVideoSet UseSecondHighestForUltra(MapVideoSet source)
    {
        // Preserve all four logical quality slots for clients capped below 1080p
        // by repeating the second-highest stream as ULTRA.
        MapVideoQuality[] qualities = [.. source.Qualities];
        qualities[^1] = qualities[^2];
        return new MapVideoSet(source.Codec, qualities);
    }
}
