namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

public class MapHistory
{
	public Guid Name { get; set; }
	public int CoachCount { get; set; }
	public bool CoopEnabled { get; set; }
	public bool SweatEnabled { get; set; }
	public string LaunchTab { get; set; } = "N\\A";
	public int Score { get; set; }
	public int Difficulty { get; set; }
	public int SweatDifficulty { get; set; }
	public bool PlayerPremiumStatus { get; set; }
	public int RemainingPlayersAtEnd { get; set; }
	public string Platform { get; set; } = "N\\A";
	public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
