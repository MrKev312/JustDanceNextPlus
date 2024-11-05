using JustDanceNextPlus.Services;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public class ContentAuthorization
{
	public int DurationSeconds { get; set; } = 3000;
	public AssetsAuthorization Assets { get; set; } = new();
	public AssetsmetadataAuthorization AssetsMetadata { get; set; } = new();
}

public class AssetsAuthorization
{
	[JsonPropertyName("audio.opus")]
	public string AudioOpus { get; set; } = "";
	[JsonPropertyName("video_HIGH.hd.webm")]
	public string VideoHighHdWebm { get; set; } = "";
	[JsonPropertyName("video_HIGH.vp9.webm")]
	public string VideoHighVP9Webm { get; set; } = "";
	[JsonPropertyName("video_LOW.hd.webm")]
	public string VideoLowHdWebm { get; set; } = "";
	[JsonPropertyName("video_LOW.vp9.webm")]
	public string VideoLowVP9Webm { get; set; } = "";
	[JsonPropertyName("video_MID.hd.webm")]
	public string VideoMidHdWebm { get; set; } = "";
	[JsonPropertyName("video_MID.vp9.webm")]
	public string VideoMidVP9Webm { get; set; } = "";
	[JsonPropertyName("video_ULTRA.hd.webm")]
	public string VideoUltraHdWebm { get; set; } = "";
	[JsonPropertyName("video_ULTRA.vp9.webm")]
	public string VideoUltraVP9Webm { get; set; } = "";
	public string MapPackage { get; set; } = "";
}

public class AssetsmetadataAuthorization
{
	[JsonIgnore]
	public WebmData[] VideoData { get; set; } = [];
	public string VideoMpd { get => UtilityService.GenerateMpd(VideoData); }
}