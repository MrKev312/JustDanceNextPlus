using JustDanceServer.JustDanceClasses.Database.Profile;

using System.ComponentModel.DataAnnotations;

namespace JustDanceServer.JustDanceClasses.Endpoints;

// Deliberately separate from DancerCard: legacy responses must retain their _id field.
public record SavedDancerCardDetails(
    string Name, string Country, Guid AvatarId, Guid PortraitBorderId, Guid AliasId,
    string AliasGender, Guid BackgroundId, Guid ScoringFxId, Guid VictoryFxId,
    IReadOnlyList<Guid> BadgesIds, IReadOnlyList<Guid> StickersIds,
    DateTime CreatedAt, DateTime UpdatedAt, Guid DancercardId)
{
    public static SavedDancerCardDetails From(DancerCard card, SavedDancerCard saved)
        => new(card.Name, card.Country, card.AvatarId, card.PortraitBorderId, card.AliasId,
            card.AliasGender, card.BackgroundId, card.ScoringFxId, card.VictoryFxId,
            [.. card.BadgesIds], [.. card.StickersIds],
            DateTime.SpecifyKind(saved.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(saved.UpdatedAt, DateTimeKind.Utc), saved.Id);

    public object ToPublic() => new
    {
        Name,
        Country,
        AvatarId,
        PortraitBorderId,
        AliasId,
        AliasGender,
        BackgroundId,
        ScoringFxId,
        VictoryFxId,
        BadgesIds,
        StickersIds,
        CreatedAt,
        UpdatedAt
    };
}

public sealed class SavedDancerCardInput
{
    [StringLength(128, MinimumLength = 1)]
    public string? Name { get; init; }
    [StringLength(3)]
    public string? Country { get; init; }
    public Guid? AvatarId { get; init; }
    public Guid? PortraitBorderId { get; init; }
    public Guid? AliasId { get; init; }
    [StringLength(32)]
    public string? AliasGender { get; init; }
    public Guid? BackgroundId { get; init; }
    public Guid? ScoringFxId { get; init; }
    public Guid? VictoryFxId { get; init; }
    public List<Guid>? BadgesIds { get; init; }
    public List<Guid>? StickersIds { get; init; }

    public void ApplyTo(DancerCard card)
    {
        if (Name is not null)
			card.Name = Name;
        if (Country is not null)
			card.Country = Country;
        if (AvatarId.HasValue)
			card.AvatarId = AvatarId.Value;
        if (PortraitBorderId.HasValue)
			card.PortraitBorderId = PortraitBorderId.Value;
        if (AliasId.HasValue)
			card.AliasId = AliasId.Value;
        if (AliasGender is not null)
			card.AliasGender = AliasGender;
        if (BackgroundId.HasValue)
			card.BackgroundId = BackgroundId.Value;
        if (ScoringFxId.HasValue)
			card.ScoringFxId = ScoringFxId.Value;
        if (VictoryFxId.HasValue)
			card.VictoryFxId = VictoryFxId.Value;
        if (BadgesIds is not null)
			card.BadgesIds = [.. BadgesIds];
        if (StickersIds is not null)
			card.StickersIds = [.. StickersIds];
	}
}
