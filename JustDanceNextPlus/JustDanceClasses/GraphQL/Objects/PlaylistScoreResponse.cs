using JustDanceNextPlus.JustDanceClasses.Database.Profile;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;

public class PlaylistStatsResponse
{
	public bool IsHighScore { get; set; }
	public required PlaylistStats PlaylistStats { get; set; }
}
