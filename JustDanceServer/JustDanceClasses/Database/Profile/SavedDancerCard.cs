namespace JustDanceServer.JustDanceClasses.Database.Profile;

// Additive storage: Profiles.Dancercard remains the primary card used by older games.
public sealed class SavedDancerCard
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string CosmeticsJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
