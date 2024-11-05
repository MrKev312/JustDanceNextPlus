namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public class Leaderboard
{
	public List<ScoreDetails> Scores { get; set; } = [];
	public long Count { get; set; }
}

public class ScoreDetails
{
	public Guid ProfileId { get; set; }
	public int Score { get; set; }
	public long Position { get; set; }
}
