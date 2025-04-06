namespace JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;

public class PlaylistInfo
{
	public bool IsHighlighted { get; set; }
	public bool IsRecommended { get; set; }
	public List<MapData> MapData { get; set; } = [];
	public Guid PlaylistId { get; set; }
	public int TotalScore { get; set; }
	public int TotalStars { get; set; }
	public bool IsCoopEnabled { get; set; }
	public bool IsGroupEnabled { get; set; }
}

public class MapData
{
	public Guid MapId { get; set; }
	public int Score { get; set; }
	public int Stars { get; set; }
}

public class PushPlaylistPlayedInput
{
	public required PlaylistInfo Playlist { get; set; }
}
