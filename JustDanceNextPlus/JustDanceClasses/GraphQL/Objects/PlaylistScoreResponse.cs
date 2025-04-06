using JustDanceNextPlus.JustDanceClasses.Database.Profile;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;

public class PlaylistStatsResponse
{
	public bool IsHighScore { get; set; }
	public required PlaylistStatsDataResponse PlaylistStats { get; set; }
}

public class PlaylistStatsDataResponse
{
	public int HighScore { get; set; }
	public int PlayCount { get; set; }
	public List<MapDataResponse> HighScorePerMap { get; set; } = [];
	public string Platform { get; set; } = "Unknown";
}

public class MapDataResponse
{
	public Guid MapId { get; set; }
	public int HighScore { get; set; }
}
