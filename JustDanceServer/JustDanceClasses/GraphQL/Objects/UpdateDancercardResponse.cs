using JustDanceServer.JustDanceClasses.Database.Profile;

namespace JustDanceServer.JustDanceClasses.GraphQL.Objects;

public class UpdateDancercardResponse
{
	public bool Success { get; set; }
	public DancerCard? Dancercard { get; set; }
}
