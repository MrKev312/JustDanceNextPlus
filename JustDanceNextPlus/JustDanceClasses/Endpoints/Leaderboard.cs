using System.Collections.Immutable;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public record Leaderboard
{
	public ImmutableArray<ScoreDetails> Scores { get; init; } = [];
	public long Count { get; init; }
}

public record ScoreDetails
{
	public Guid ProfileId { get; init; }
	public int Score { get; init; }
	public long Position { get; init; }
}
