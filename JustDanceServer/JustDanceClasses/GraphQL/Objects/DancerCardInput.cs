using JustDanceServer.JustDanceClasses.Database.Profile;

namespace JustDanceServer.JustDanceClasses.GraphQL.Objects;

public abstract class DancerCardInput
{
	public string? Name { get; set; }
	public string? Country { get; set; }
	public Guid? AvatarId { get; set; }
	public Guid? PortraitBorderId { get; set; }
	public Guid? AliasId { get; set; }
	public string? AliasGender { get; set; }
	public Guid? BackgroundId { get; set; }
	public Guid? ScoringFxId { get; set; }
	public Guid? VictoryFxId { get; set; }
	public List<Guid>? BadgesIds { get; set; }
	public List<Guid>? StickersIds { get; set; }

	public DancerCard ToDancerCard(Guid id, DancerCard current)
	{
		return new DancerCard
		{
			Id = id,
			Name = Name ?? current.Name,
			Country = Country ?? current.Country,
			AvatarId = AvatarId ?? current.AvatarId,
			PortraitBorderId = PortraitBorderId ?? current.PortraitBorderId,
			AliasId = AliasId ?? current.AliasId,
			AliasGender = AliasGender ?? current.AliasGender,
			BackgroundId = BackgroundId ?? current.BackgroundId,
			ScoringFxId = ScoringFxId ?? current.ScoringFxId,
			VictoryFxId = VictoryFxId ?? current.VictoryFxId,
			BadgesIds = BadgesIds == null ? [.. current.BadgesIds] : [.. BadgesIds],
			StickersIds = StickersIds == null ? [.. current.StickersIds] : [.. StickersIds]
		};
	}
}
