namespace JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;

public class MapInfo
{
	public Guid MapId { get; set; }
	public int Score { get; set; }
	public string GameMode { get; set; } = "N/A";
	public double CompletionPercentage { get; set; }
	public int MissedMovesCount { get; set; }
	public int OkayMovesCount { get; set; }
	public int GoodMovesCount { get; set; }
	public int SuperMovesCount { get; set; }
	public int PerfectMovesCount { get; set; }
	public int GoldMovesCount { get; set; }
	public List<bool> GoldMovesAchieved { get; set; } = [];
	public int Stars { get; set; }
	public bool IsGroupEnabled { get; set; }
	public bool IsRecommended { get; set; }
	public bool IsCoopEnabled { get; set; }
	public string LaunchTab { get; set; } = "Unknown";
	public int RemainingPlayersAtTheEnd { get; set; }
}

public class PushMapsPlayedInput
{
	public MapInfo[] Maps { get; set; } = [];
}