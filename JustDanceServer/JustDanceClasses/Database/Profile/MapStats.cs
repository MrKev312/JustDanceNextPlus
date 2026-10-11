using System.Text.Json.Serialization;

namespace JustDanceServer.JustDanceClasses.Database.Profile;

public class MapStats
{
    [JsonIgnore]
    public string MapName { get; set; } = string.Empty;

    [JsonIgnore]
    public Guid ProfileId { get; set; }

    public int HighScore { get; set; }
    public int PlayCount { get; set; }
    public string Platform { get; set; } = "Unknown";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCoopHighScore { get; set; }

    public HighscorePerformance HighScorePerformance { get; set; } = new();
    public MoveCounts MoveCount => HighScorePerformance.Moves;
    public GameModeStats? GameModeStats { get; set; }
}

public class HighscorePerformance
{
    public List<bool> GoldMovesAchieved { get; set; } = [];
    public MoveCounts Moves { get; set; } = new();
}

public class MoveCounts
{
    public int Missed { get; set; }
    public int Okay { get; set; }
    public int Good { get; set; }
    public int Super { get; set; }
    public int Perfect { get; set; }
    public int Gold { get; set; }
}

public class GameModeStats
{
    [JsonIgnore]
    public bool Exists { get; set; } = true;
    public ChallengeStats Challenge { get; set; } = new();
}

public class ChallengeStats
{
    public int LastScore { get; set; }
}
