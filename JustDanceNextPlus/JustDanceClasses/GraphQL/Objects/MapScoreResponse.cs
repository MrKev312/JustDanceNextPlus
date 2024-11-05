using JustDanceNextPlus.JustDanceClasses.Database.Profile;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;

public class MapScoreResponse
{
	public bool Success { get; set; }
	public ResponseData Response { get; set; } = new();
}

public class ResponseData
{
	public int EarnedXP { get; set; }
	public bool IsHighScore { get; set; }
	public int CurrentXP { get; set; }
	public int CurrentLevel { get; set; }
	public bool IsLevelIncreased { get; set; }
	public bool IsPrestigeIncreased { get; set; }
	public int PrestigeGrade { get; set; }
	public MapStats MapStats { get; set; } = new();
	public int EarnedSeasonPoints { get; set; }
}
