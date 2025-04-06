using JustDanceNextPlus.JustDanceClasses.Database.Profile;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;

public class MapStatsResponse
{
	public int EarnedXP { get; set; }
	public bool IsHighScore { get; set; }
	public int CurrentXP { get; set; }
	public int CurrentLevel { get; set; }
	public bool IsLevelIncreased { get; set; }
	public bool IsPrestigeIncreased { get; set; }
	public int PrestigeGrade { get; set; }
	public required MapStats MapStats { get; set; }
	public int EarnedSeasonPoints { get; set; }
}
