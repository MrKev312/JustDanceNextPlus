using Microsoft.EntityFrameworkCore;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

[PrimaryKey("MapId", "ProfileId")]
public class MapStats
{
	[JsonIgnore]
	public Guid MapId { get; set; }

	[JsonIgnore]
	public Guid ProfileId { get; set; }

	public int HighScore { get; set; }
	public long PlayCount { get; set; }
	public string Platform { get; set; } = "Unknown";
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public HighscorePerformance HighScorePerformance { get; set; } = new();
	public GameModeStats? GameModeStats { get; set; }
}

[Owned]
public class HighscorePerformance
{
	public bool[] GoldMovesAchieved { get; set; } = [];
	public MoveCounts Moves { get; set; } = new();
}

[Owned]
public class MoveCounts
{
	public int Missed { get; set; }
	public int Okay { get; set; }
	public int Good { get; set; }
	public int Super { get; set; }
	public int Perfect { get; set; }
	public int Gold { get; set; }
}

[Owned]
public class GameModeStats
{
	[JsonIgnore]
	public bool Exists { get; set; } = true;
	public ChallengeStats Challenge { get; set; } = new();
}

[Owned]
public class ChallengeStats
{
	public int LastScore { get; set; }
}
