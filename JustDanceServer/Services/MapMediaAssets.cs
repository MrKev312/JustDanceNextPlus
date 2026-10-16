using JustDanceServer.Utilities;

namespace JustDanceServer.Services;

public enum MapVideoCodec
{
    Vp8,
    Vp9
}

public static class MapVideoCodecExtensions
{
    public static string ToCodecName(this MapVideoCodec codec)
        => codec switch
        {
            MapVideoCodec.Vp8 => "vp8",
            MapVideoCodec.Vp9 => "vp9",
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unsupported map video codec.")
        };

    public static MapVideoCodec ParseCodecName(string codec)
        => codec.ToLowerInvariant() switch
        {
            "vp8" => MapVideoCodec.Vp8,
            "vp9" => MapVideoCodec.Vp9,
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unsupported map video codec.")
        };
}

public sealed record MapVideoQuality(WebmData Data, string Url);

public sealed record MapVideoSet
{
    private readonly IReadOnlyList<MapVideoQuality> availableQualities;

    public MapVideoSet(MapVideoCodec codec, IReadOnlyList<MapVideoQuality> qualities)
    {
        ArgumentNullException.ThrowIfNull(qualities);

        if (qualities.Count == 0)
            throw new ArgumentException("A video set must contain at least one video.", nameof(qualities));

        if (qualities.Zip(qualities.Skip(1)).Any(pair => pair.First.Data.Bitrate > pair.Second.Data.Bitrate))
            throw new ArgumentException("Video qualities must be ordered by ascending bitrate.", nameof(qualities));

        Codec = codec;
        availableQualities = Array.AsReadOnly(qualities.ToArray());
        Qualities = availableQualities;
    }

    private MapVideoSet(MapVideoSet source, IReadOnlyList<MapVideoQuality> qualities)
    {
        Codec = source.Codec;
        availableQualities = source.availableQualities;
        Qualities = qualities;
    }

    public MapVideoCodec Codec { get; }

    public IReadOnlyList<MapVideoQuality> Qualities { get; }

    public MapVideoSet WithQualityCount(int qualityCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(qualityCount);

        if (qualityCount == Qualities.Count)
            return this;

        List<MapVideoQuality> slots = [.. availableQualities.Take(qualityCount)];

        // A shorter client layout must still expose the highest-quality source.
        if (availableQualities.Count > qualityCount)
            slots[^1] = availableQualities[^1];

        while (slots.Count < qualityCount)
            slots.Add(slots[^1]);

        return new MapVideoSet(this, slots.AsReadOnly());
    }
}

public sealed record MapMediaAssets(
    MapMediaLayout Layout,
    string PreviewAudioUrl,
    string FullAudioUrl,
    IReadOnlyDictionary<string, MapVideoSet> PreviewVideos,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PlatformAssets)
{
    public MapVideoSet GetPreviewVideos(string codec)
        => PreviewVideos.TryGetValue(codec, out MapVideoSet? videos)
            ? videos.WithQualityCount(VideoQualityLayout.Standard.Count)
            : throw new InvalidDataException(
                $"{Layout.FolderName} is missing its required {codec} preview videos.");

    public IReadOnlyDictionary<string, string> GetPlatformAssets(string platform)
        => PlatformAssets.TryGetValue(platform, out IReadOnlyDictionary<string, string>? assets)
            ? assets
            : throw new InvalidDataException(
                $"{Layout.FolderName} is missing its required {platform} platform assets.");
}
