using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public record ContentAuthorization
{
	public int DurationSeconds { get; init; } = 3000;
	public AssetsAuthorization Assets { get; init; } = new();
	public AssetsmetadataAuthorization AssetsMetadata { get; init; } = new();
}

public record AssetMetadata
{
	public AssetMetadata() { }

	public AssetMetadata(string hash, long size)
	{
		Size = size;
		Hash = hash;
	}

	public AssetMetadata(string path)
	{
		Size = new FileInfo(path).Length;
		Hash = Path.GetFileNameWithoutExtension(path);
	}

	public long Size { get; init; }
	public string Hash { get; init; } = "";
}

public record AssetsAuthorization
{
	[JsonPropertyName("audio.opus")]
	public string AudioOpus { get; init; } = "";
	[JsonPropertyName("video_HIGH.hd.webm")]
	public string VideoHighHdWebm { get; init; } = "";
	[JsonPropertyName("video_HIGH.vp9.webm")]
	public string VideoHighVP9Webm { get; init; } = "";
	[JsonPropertyName("video_LOW.hd.webm")]
	public string VideoLowHdWebm { get; init; } = "";
	[JsonPropertyName("video_LOW.vp9.webm")]
	public string VideoLowVP9Webm { get; init; } = "";
	[JsonPropertyName("video_MID.hd.webm")]
	public string VideoMidHdWebm { get; init; } = "";
	[JsonPropertyName("video_MID.vp9.webm")]
	public string VideoMidVP9Webm { get; init; } = "";
	[JsonPropertyName("video_ULTRA.hd.webm")]
	public string VideoUltraHdWebm { get; init; } = "";
	[JsonPropertyName("video_ULTRA.vp9.webm")]
	public string VideoUltraVP9Webm { get; init; } = "";
	public string MapPackage { get; init; } = "";
}

public record AssetsmetadataAuthorization
{
	[JsonIgnore]
	public WebmData[] VideoData { get; init; } = [];
	public string VideoMpd { get => UtilityService.GenerateMpd(VideoData); }
}