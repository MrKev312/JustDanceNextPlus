namespace JustDanceServer.JustDanceClasses.Database.Profile;

public enum FriendshipState
{
    Pending,
    Accepted
}

/// <summary>
/// One normalized relationship between two local Just Dance players.
/// FirstProfileId is always the smaller GUID, so there can only be one row for
/// a pair. RequestedByProfileId supplies the direction while the row is pending.
/// </summary>
public sealed class Friendship
{
    public Guid FirstProfileId { get; set; }
    public Guid SecondProfileId { get; set; }
    public Guid RequestedByProfileId { get; set; }
    public FriendshipState State { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
