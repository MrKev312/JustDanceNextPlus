namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public record Party
{
	public required Guid PartyId { get; init; }

	public List<Guid> PartyMembers { get; init; } = [];
}
