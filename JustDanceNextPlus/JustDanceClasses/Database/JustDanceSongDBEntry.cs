using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using System.Collections.Immutable;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

[DynamicLinqType]
public record JustDanceSongDBEntry
{
	public string Artist { get; init; } = "";
	public AssetsMetadata AssetsMetadata { get; init; } = new();
	public int CoachCount { get; init; } = 0;
	public ImmutableArray<OasisTag> CoachNamesLocIds { get; init; } = [];
	public string Credits { get; init; } = "";
	public required OasisTag DanceVersionLocId { get; init; }
	public int Difficulty { get; init; } = 0;
	public object? DoubleScoringType { get; init; } = null;
	public bool HasSongTitleInCover => Assets.SongTitleLogo != null;
	public bool HasCameraScoring { get; init; } = false;
	public string LyricsColor { get; init; } = "";
	public float MapLength { get; init; } = 0;
	public string MapName { get; init; } = "";
	public int OriginalJDVersion { get; init; } = 0;
	public string ParentMapName { get; init; } = "";
	public int SweatDifficulty { get; init; } = 0;
	public ImmutableArray<GuidTag> TagIds { get; init; } = [];
	public ImmutableArray<string> Tags { get; init; } = [];
	public string Title { get; init; } = "";
	public SongDBAssets Assets { get; init; } = new();
}

public record AssetsMetadata
{
    [JsonConverter(typeof(JsonStringConverter))]
    public AudioPreviewTrk? AudioPreviewTrk { get; init; }

    [JsonIgnore]
	public ImmutableArray<WebmData> VideoData { get; init; } = [];
	public string VideoPreviewMpd => UtilityService.GenerateMpd(VideoData, true);
}

public record AudioPreviewTrk
{
	public float StartBeat { get; init; }
	public float EndBeat { get; init; }
	public float PreviewDuration { get; init; }
	public ImmutableArray<int> Markers { get; init; } = [];
	public float VideoStartTime { get; init; }
	public float PreviewLoopStart { get; init; }
	public float PreviewEntry { get; init; }
}

public record SongDBAssets
{
	[JsonPropertyName("audioPreview.opus")]
	public string? AudioPreview_opus { get; init; }
	[JsonPropertyName("videoPreview_HIGH.vp8.webm")]
	public string? VideoPreview_HIGH_vp8_webm { get; init; }
	[JsonPropertyName("videoPreview_HIGH.vp9.webm")]
	public string? VideoPreview_HIGH_vp9_webm { get; init; }
	[JsonPropertyName("videoPreview_LOW.vp8.webm")]
	public string? VideoPreview_LOW_vp8_webm { get; init; }
	[JsonPropertyName("videoPreview_LOW.vp9.webm")]
	public string? VideoPreview_LOW_vp9_webm { get; init; }
	[JsonPropertyName("videoPreview_MID.vp8.webm")]
	public string? VideoPreview_MID_vp8_webm { get; init; }
	[JsonPropertyName("videoPreview_MID.vp9.webm")]
	public string? VideoPreview_MID_vp9_webm { get; init; }
	[JsonPropertyName("videoPreview_ULTRA.vp8.webm")]
	public string? VideoPreview_ULTRA_vp8_webm { get; init; }
	[JsonPropertyName("videoPreview_ULTRA.vp9.webm")]
	public string? VideoPreview_ULTRA_vp9_webm { get; init; }
	public string? CoachesLarge { get; init; }
	public string? CoachesSmall { get; init; }
	public string? Cover { get; init; }

	// These aren't needed
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Cover1024 { get; init; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? CoverSmall { get; init; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? SongTitleLogo { get; init; }
}
