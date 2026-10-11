using JustDanceServer.Configuration;
using JustDanceServer.Services;

namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record PartyResponse
{
    public required Guid PartyId { get; init; }
    public Guid PartyTypeId { get; init; } = JdNextConstants.PartyTypeId;
    public required Guid OwnerProfileId { get; init; }
    public Guid SpaceId { get; init; } = JdNextConstants.GameSpaceId;
    public Guid GroupId { get; init; } = JdNextConstants.PartyGroupId;
    public object Obj { get; init; } = new();
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastModifiedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public string LockState { get; init; } = "unlocked";
    public required string Revision { get; init; }
    public bool CrossPlayEnabled { get; init; } = true;
    public string[] RestrictedPlatforms { get; init; } =
        ["NX", "PC", "ORBIS", "PS5", "DURANGO", "XBX", "XboxScarlett"];
}

public sealed record PartyMemberResponse
{
    public required Guid ProfileId { get; init; }
    public object Obj { get; init; } = new();
    public required DateTime AddedAt { get; init; }
    public required DateTime LastModifiedAt { get; init; }
    public required long Revision { get; init; }
}

public static class PartyWireProjection
{
    public static PartyResponse ToResponse(PartySnapshot party)
        => new()
        {
            PartyId = party.PartyId,
            OwnerProfileId = party.OwnerProfileId,
            CreatedAt = party.CreatedAt,
            LastModifiedAt = party.LastModifiedAt,
            ExpiresAt = party.ExpiresAt,
            Revision = party.Revision.ToString()
        };
}
