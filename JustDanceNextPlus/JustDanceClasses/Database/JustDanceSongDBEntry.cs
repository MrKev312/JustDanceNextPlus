using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public class JustDanceSongDBEntry
{
	public string Artist { get; set; } = "";
	public Assetsmetadata AssetsMetadata { get; set; } = new();
	public int CoachCount { get; set; } = 0;
	public OasisTag[] CoachNamesLocIds { get; set; } = [];
	public string Credits { get; set; } = "";
	public int DanceVersionLocId { get; set; } = 0;
	public int Difficulty { get; set; } = 0;
	public object? DoubleScoringType { get; set; } = null;
	public bool HasSongTitleInCover => Assets.SongTitleLogo != null;
	public string LyricsColor { get; set; } = "";
	public float MapLength { get; set; } = 0;
	public string MapName { get; set; } = "";
	public int OriginalJDVersion { get; set; } = 0;
	public string ParentMapName { get; set; } = "";
	public int SweatDifficulty { get; set; } = 0;
	public HashSet<GuidTag> TagIds { get; set; } = [];
	public HashSet<string> Tags { get; set; } = [];
	public string Title { get; set; } = "";
	public Assets Assets { get; set; } = new();
}

public class Assetsmetadata
{
	public string? AudioPreviewTrk { get; set; }

	[JsonIgnore]
	public WebmData[] VideoData { get; set; } = [];
	public string VideoPreviewMpd { get => UtilityService.GenerateMpd(VideoData, true); }
}

public class AudioPreviewTrk
{
	public float StartBeat { get; set; }
	public float EndBeat { get; set; }
	public float PreviewDuration { get; set; }
	public int[] Markers { get; set; } = [];
	public float VideoStartTime { get; set; }
	public float PreviewLoopStart { get; set; }
	public float PreviewEntry { get; set; }
}

public class Assets
{
	[JsonPropertyName("audioPreview.opus")]
	public string? AudioPreview_opus { get; set; }
	[JsonPropertyName("videoPreview_HIGH.vp8.webm")]
	public string? VideoPreview_HIGH_vp8_webm { get; set; }
	[JsonPropertyName("videoPreview_HIGH.vp9.webm")]
	public string? VideoPreview_HIGH_vp9_webm { get; set; }
	[JsonPropertyName("videoPreview_LOW.vp8.webm")]
	public string? VideoPreview_LOW_vp8_webm { get; set; }
	[JsonPropertyName("videoPreview_LOW.vp9.webm")]
	public string? VideoPreview_LOW_vp9_webm { get; set; }
	[JsonPropertyName("videoPreview_MID.vp8.webm")]
	public string? VideoPreview_MID_vp8_webm { get; set; }
	[JsonPropertyName("videoPreview_MID.vp9.webm")]
	public string? VideoPreview_MID_vp9_webm { get; set; }
	[JsonPropertyName("videoPreview_ULTRA.vp8.webm")]
	public string? VideoPreview_ULTRA_vp8_webm { get; set; }
	[JsonPropertyName("videoPreview_ULTRA.vp9.webm")]
	public string? VideoPreview_ULTRA_vp9_webm { get; set; }
	public string? CoachesLarge { get; set; }
	public string? CoachesSmall { get; set; }
	public string? Cover { get; set; }

	// These aren't needed
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Cover1024 { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? CoverSmall { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? SongTitleLogo { get; set; }
}
