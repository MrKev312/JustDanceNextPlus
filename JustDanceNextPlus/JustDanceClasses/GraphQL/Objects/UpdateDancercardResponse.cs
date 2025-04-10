using JustDanceNextPlus.JustDanceClasses.Database.Profile;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;

public class UpdateDancercardResponse
{
	public bool Success { get; set; }
	public DancerCard? Dancercard { get; set; }
}