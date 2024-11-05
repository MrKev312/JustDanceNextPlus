namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public class Party
{
	public required Guid PartyId { get; set; }

	public List<Guid> PartyMembers { get; set; } = [];
}
