using System.Text.Json.Serialization;

namespace JustDanceServer.JustDanceClasses.Endpoints;

public record JduContentAuthorizationEntry
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "ContentAuthorizationEntry";
    public int Duration { get; init; } = 300;
    public int Changelist { get; init; }
    public Dictionary<string, string> Urls { get; init; } = [];
}

public record JduSongDatabaseEntry
{
    public string Artist { get; init; } = string.Empty;
    public Dictionary<string, string> Assets { get; init; } = [];
    public string AudioPreviewData { get; init; } = string.Empty;
    public int CoachCount { get; init; }
    public string Credits { get; init; } = string.Empty;
    public string CustomTypeName { get; init; } = string.Empty;
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? CustomTypeNameId { get; init; }
    public int Difficulty { get; init; }
    public string[] JdmAttributes { get; init; } = [];
    public string LyricsColor { get; init; } = "FFFFFFFF";
    public int LyricsType { get; init; }
    public int MainCoach { get; init; } = -1;
    public double MapLength { get; init; }
    public string MapName { get; init; } = string.Empty;
    public string MapPreviewMpd { get; init; } = string.Empty;
    public int Mode { get; init; } = 6;
    public int OriginalJDVersion { get; init; }
    public Dictionary<string, string> Packages { get; init; } = [];
    public string ParentMapName { get; init; } = string.Empty;
    public string[] SearchTags { get; init; } = [];
    public int[] SearchTagsLocIds { get; init; } = [];
    public int ServerChangelist { get; init; }
    public string[] SkuIds { get; init; } = [];
    public Dictionary<string, string> SongColors { get; init; } = [];
    public int Status { get; init; } = 3;
    public int SweatDifficulty { get; init; }
    public string[] Tags { get; init; } = [];
    public string Title { get; init; } = string.Empty;
    public Dictionary<string, string> Urls { get; init; } = [];
    public bool Wdf { get; init; }
    public bool IsUplayTimeUnlocked { get; init; }
}

public record JduHomeDataResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "HomeService::HomeData";
    public JduHomeTile[] TileList { get; init; } = [];
    public long TimestampLastRequest { get; init; }
    public long TimestampLastManualContent { get; init; }
    public long TimestampLastMapContent { get; init; }
    public long TimestampLastVideoContent { get; init; }
    public long TimestampLastPlaylistContent { get; init; }
    public long TimestampLastLocalTrack { get; init; }
}

public record JduHomeTile
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "HomeService::HomeTile";
    public int Type { get; init; }
    public long CreationTime { get; init; }
    public bool Locked { get; init; }
    public bool New { get; init; }
    public long LockDuration { get; init; }
    public long ContentExpiry { get; init; }
    public Guid Uuid { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JduNewsTileInfo? NewsTileInfo { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JduMapTileInfo? MapTileInfo { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JduPlaylistTileInfo? PlaylistTileInfo { get; init; }
}

public record JduNewsTileInfo
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "HomeService::NewsTileInfo";
    public int Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string TrackingTag { get; init; } = string.Empty;
    public Guid WinnerPid { get; init; }
    public string WinnerPlatform { get; init; } = string.Empty;
    public string WinnerName { get; init; } = string.Empty;
    public uint WinnerNameSuffix { get; init; }
    public uint WinnerCountry { get; init; } = uint.MaxValue;
    public uint WinnerAvatar { get; init; } = uint.MaxValue;
    public uint WinnerAlias { get; init; } = uint.MaxValue;
    public string OfflineNewID { get; init; } = string.Empty;
}

public record JduMapTileInfo
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "HomeService::MapTileInfo";
    public string MapName { get; init; } = string.Empty;
    public int Type { get; init; } = 3;
}

public record JduPlaylistTileInfo
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "HomeService::PlaylistTileInfo";
    public int Type { get; init; }
    public string PlaylistID { get; init; } = string.Empty;
}

public record JduPlaylistDbResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "PlaylistDbResponse";
    public Dictionary<string, JduPlaylist> Db { get; init; } = [];
}

public record JduPlaylist
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "PlaylistDbService::Playlist";
    public bool Filters { get; init; } = true;
	public string Type { get; init; } = "editorial";
    public string[] Maps { get; init; } = [];
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CoverURL { get; init; } = string.Empty;
    public bool FixedMapOrder { get; init; }
    public bool Fallback { get; init; }
    public bool Pinned { get; init; }
}

public record JduCarouselContentResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "JD_CarouselContent";
    public JduCarouselCategory[] Categories { get; init; } = [];
    public Dictionary<string, JduCarouselActionList> ActionLists { get; init; } = [];
    public Dictionary<string, object> SongItemLists { get; init; } = [];
}

public record JduCarouselCategory
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "Category";
    public string Title { get; init; } = string.Empty;
    public string Act { get; init; } = "ui_carousel";
    public string Isc { get; init; } = "grp_row";
    public string CategoryType { get; init; } = "playlist";
    public JduCarouselItem[] Items { get; init; } = [];
}

public record JduCarouselItem
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "Item";
    public string Isc { get; init; } = "grp_row";
    public string Act { get; init; } = "ui_carousel";
    public string ActionList { get; init; } = "_None";
    public string ActionListUpsell { get; init; } = "_None";
    public JduCarouselPlaylistComponent[] Components { get; init; } = [];
}

public record JduCarouselPlaylistComponent
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "JD_CarouselContentComponent_Playlist";
    public string PlaylistID { get; init; } = string.Empty;
    public int DisplayCode { get; init; } = 1;
    public string DisplayMethod { get; init; } = "manual";
}

public record JduCarouselActionList
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "ActionList";
    public JduCarouselAction[] Actions { get; init; } = [];
    public string ItemType { get; init; } = "map";
}

public record JduCarouselAction
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "Action";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BannerContext { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BannerTheme { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BannerType { get; init; }

    public string Title { get; init; } = "None";
    public string Type { get; init; } = "do-nothing";
}

public record JduSongCarouselContentResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "JD_CarouselContent";
    public JduSongCarouselCategory[] Categories { get; init; } = [];
    public Dictionary<string, JduCarouselActionList> ActionLists { get; init; } = [];
    public Dictionary<string, object> SongItemLists { get; init; } = [];
}

public record JduSongCarouselCategory
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "Category";
    public string Title { get; init; } = string.Empty;
    public string Act { get; init; } = "ui_carousel";
    public string Isc { get; init; } = "grp_row";
    public JduSongCarouselItem[] Items { get; init; } = [];
}

public record JduSongCarouselItem
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "Item";
    public string Isc { get; init; } = "grp_cover";
    public string Act { get; init; } = "ui_component_base";
    public JduCarouselContentComponent[] Components { get; init; } = [];
    public string ActionList { get; init; } = "partyMap";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EndPos { get; init; }
}

public record JduAnthologyCarouselContentResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "JD_CarouselContent";
    public JduAnthologyCarouselCategory[] Categories { get; init; } = [];
    public Dictionary<string, JduCarouselActionList> ActionLists { get; init; } = [];
    public Dictionary<string, object> SongItemLists { get; init; } = [];
}

public record JduAnthologyCarouselCategory
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "Category";
    public string Title { get; init; } = string.Empty;
    public string Act { get; init; } = "ui_carousel";
    public string Isc { get; init; } = "anthology_grid";
    public JduAnthologyCarouselItem[] Items { get; init; } = [];
}

public record JduAnthologyCarouselItem
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "Item";
    public string Isc { get; init; } = string.Empty;
    public string Act { get; init; } = "ui_component_base";
    public JduCarouselContentComponent[] Components { get; init; } = [];
    public string ActionList { get; init; } = "_None";
}

public record JduCarouselContentComponent
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MapName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CategoryId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Episode { get; init; }
}

public record JduCarouselSongComponent : JduCarouselContentComponent
{
    public JduCarouselSongComponent()
        => Class = "JD_CarouselContentComponent_Song";
}

public record JduCarouselShuffleComponent : JduCarouselContentComponent
{
    public JduCarouselShuffleComponent()
        => Class = "JD_CarouselContentComponent_Shuffle";
}

public record JduCarouselPlaylistItemComponent : JduCarouselContentComponent
{
    public JduCarouselPlaylistItemComponent()
        => Class = "JD_CarouselContentComponent_Playlist";
}

public record JduCarouselEpisodeComponent : JduCarouselContentComponent
{
    public JduCarouselEpisodeComponent()
        => Class = "JD_CarouselContentComponent_Episode";
}

public sealed record JduCarouselRequest
{
	public Guid[] FriendIds { get; init; } = [];
	public Guid[] AuthorizedFriendIds { get; init; } = [];
	public string[] SearchTags { get; init; } = [];
	public string SearchString { get; init; } = string.Empty;
	public string LastMapPlayed { get; init; } = string.Empty;
}
