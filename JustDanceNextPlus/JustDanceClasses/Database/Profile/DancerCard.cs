using HotChocolate;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

public class DancerCard
{
	[GraphQLIgnore]
	public Guid Id { get; set; } = Guid.Empty;

	public string Name { get; set; } = "John Doe";
	public string Country { get; set; } = "";
	public Guid AvatarId { get; set; }
	public Guid PortraitBorderId { get; set; }
	public Guid AliasId { get; set; }
	public string AliasGender { get; set; } = "NonBinary";
	public Guid BackgroundId { get; set; }
	public Guid ScoringFxId { get; set; }
	public Guid VictoryFxId { get; set; }
	public List<Guid> BadgesIds { get; set; } = [];
	public List<Guid> StickersIds { get; set; } = [Guid.Parse("92447018-ff05-4cef-b873-dc9d1b533e89"), Guid.Parse("88c7a052-bdf5-4201-8b2a-e6ee866f0872"), Guid.Parse("335ce432-e2ba-474f-8467-588f3c6bd16c"), Guid.Parse("6032ca01-8df6-4718-acaf-63adf8758bc7"), Guid.Parse("a8cca1f8-73c8-4422-8fce-ea6cac70fcac"), Guid.Parse("40f8e714-002d-4528-b03a-3a3527f06f7a")];
}