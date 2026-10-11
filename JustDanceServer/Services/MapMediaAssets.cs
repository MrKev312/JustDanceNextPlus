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
	public const int QualityCount = 4;
	public static IReadOnlyList<string> QualityNames { get; } = ["LOW", "MID", "HIGH", "ULTRA"];

	public MapVideoSet(MapVideoCodec codec, IReadOnlyList<MapVideoQuality> qualities)
	{
		if (qualities.Count != QualityCount)
			throw new ArgumentException($"A video set must contain {QualityCount} ordered qualities.", nameof(qualities));
		if (qualities.Zip(qualities.Skip(1)).Any(pair => pair.First.Data.Bitrate > pair.Second.Data.Bitrate))
			throw new ArgumentException("Video qualities must be ordered by ascending bitrate.", nameof(qualities));

		Codec = codec;
		Qualities = [.. qualities];
	}

	public MapVideoCodec Codec { get; }
	public IReadOnlyList<MapVideoQuality> Qualities { get; }
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
			? videos
			: throw new InvalidDataException(
				$"{Layout.FolderName} is missing its required {codec} preview videos.");

	public IReadOnlyDictionary<string, string> GetPlatformAssets(string platform)
		=> PlatformAssets.TryGetValue(platform, out IReadOnlyDictionary<string, string>? assets)
			? assets
			: throw new InvalidDataException(
				$"{Layout.FolderName} is missing its required {platform} platform assets.");
}
