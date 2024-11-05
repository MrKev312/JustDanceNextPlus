namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

public class MapStats : PlayerMapData
{
	public int HighScore { get; set; }
	public long PlayCount { get; set; }
	public string Platform { get; set; } = "Unknown";
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	public virtual HighscorePerformance? HighScorePerformance { get; set; }
	public virtual GameModeStats? GameModeStats { get; set; }
}

public class HighscorePerformance : PlayerMapData
{
	public bool[] GoldMovesAchieved { get; set; } = [];
	public virtual MoveCounts Moves { get; set; } = new();
}

public class MoveCounts : PlayerMapData
{
	public int Missed { get; set; }
	public int Okay { get; set; }
	public int Good { get; set; }
	public int Super { get; set; }
	public int Perfect { get; set; }
	public int Gold { get; set; }
}

public class GameModeStats : PlayerMapData
{
	public virtual ChallengeStats? Challenge { get; set; }
}

public class ChallengeStats : PlayerMapData
{
	public int LastScore { get; set; }
}
