using JustDanceServer.Configuration;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace JustDanceServer.Services;

public sealed record PartyMemberSnapshot(
    Guid ProfileId,
    DateTime CreatedAt,
    DateTime LastModifiedAt,
    long Revision);

public sealed record PartyInvitationSnapshot(
    Guid ProfileId,
    Guid InvitedByProfileId,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public sealed record PartySnapshot(
    Guid PartyId,
    Guid OwnerProfileId,
    DateTime CreatedAt,
    DateTime LastModifiedAt,
    DateTime ExpiresAt,
    long Revision,
    IReadOnlyList<PartyMemberSnapshot> Members,
    IReadOnlyList<PartyInvitationSnapshot> Invitations);

public sealed record PartyAccessToken(string Value, DateTime CreatedAt, DateTime ExpiresAt);

public interface IPartyService
{
    PartySnapshot Create(Guid ownerProfileId);
    PartySnapshot? Get(Guid partyId);
    PartySnapshot? GetByProfileId(Guid profileId);
    PartyAccessToken? IssueAccessToken(Guid partyId, Guid ownerProfileId, Guid? intendedProfileId);
    bool Join(Guid partyId, Guid profileId, string accessToken);
    bool Leave(Guid partyId, Guid profileId, Guid actorProfileId);
    bool ExtendExpiry(Guid partyId, Guid ownerProfileId, TimeSpan duration, out PartySnapshot? party);
    bool SetAutoRemove(Guid partyId, Guid profileId, bool enabled);
    void HandleDisconnect(Guid profileId);
    bool Invite(Guid partyId, Guid ownerProfileId, Guid profileId);
    bool RemoveInvitation(Guid partyId, Guid actorProfileId, Guid profileId);
}

public sealed class PartyService(IUbiServicesNotificationHub notifications) : IPartyService
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromMinutes(30);

    private readonly Lock stateLock = new();
    private readonly Dictionary<Guid, PartyState> parties = [];
    private readonly Dictionary<Guid, Guid> partyByProfile = [];
    public PartySnapshot Create(Guid ownerProfileId)
    {
        if (ownerProfileId == Guid.Empty)
            throw new ArgumentException("A party owner is required.", nameof(ownerProfileId));

        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (GetByProfileIdLocked(ownerProfileId) is PartySnapshot existing)
                return existing;

            DateTime now = DateTime.UtcNow;
            PartyState state = new()
            {
                PartyId = Guid.NewGuid(),
                OwnerProfileId = ownerProfileId,
                CreatedAt = now,
                LastModifiedAt = now,
                ExpiresAt = now.Add(DefaultLifetime),
                Revision = 1
            };
            state.Members.Add(ownerProfileId, new PartyMemberState(now, now, 1));
            parties.Add(state.PartyId, state);
            partyByProfile[ownerProfileId] = state.PartyId;
            PublishPartyCreated(state);
            return ToSnapshot(state);
        }
    }

    public PartySnapshot? Get(Guid partyId)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            return parties.TryGetValue(partyId, out PartyState? state)
                ? ToSnapshot(state)
                : null;
        }
    }

    public PartySnapshot? GetByProfileId(Guid profileId)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            return GetByProfileIdLocked(profileId);
        }
    }

    public PartyAccessToken? IssueAccessToken(
        Guid partyId,
        Guid ownerProfileId,
        Guid? intendedProfileId)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (!parties.TryGetValue(partyId, out PartyState? state)
                || state.OwnerProfileId != ownerProfileId)
                return null;

            DateTime now = DateTime.UtcNow;
            string rawToken = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
            state.AccessTokens[HashToken(rawToken)] = new PartyTokenState(
                intendedProfileId,
                now,
                now.Add(AccessTokenLifetime));
            return new PartyAccessToken(rawToken, now, now.Add(AccessTokenLifetime));
        }
    }

    public bool Join(Guid partyId, Guid profileId, string accessToken)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (profileId == Guid.Empty
                || partyByProfile.ContainsKey(profileId)
                || !parties.TryGetValue(partyId, out PartyState? state)
                || !state.AccessTokens.Remove(HashToken(accessToken), out PartyTokenState? token)
                || token.ExpiresAt <= DateTime.UtcNow
                || (token.IntendedProfileId is Guid intended && intended != profileId))
                return false;

            DateTime now = DateTime.UtcNow;
            state.Invitations.Remove(profileId);
            TouchLocked(state, now);
            state.Members.Add(profileId, new PartyMemberState(now, now, state.Revision));
            partyByProfile[profileId] = partyId;
            PublishMemberAdded(state, profileId, now);
            return true;
        }
    }

    public bool Leave(Guid partyId, Guid profileId, Guid actorProfileId)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (!parties.TryGetValue(partyId, out PartyState? state)
                || !state.Members.ContainsKey(profileId)
                || (actorProfileId != profileId && actorProfileId != state.OwnerProfileId))
                return false;

            Guid[] recipients = [.. state.Members.Keys];
            state.Members.Remove(profileId);
            partyByProfile.Remove(profileId);
            if (state.Members.Count == 0)
            {
                RemovePartyLocked(state, recipients);
                return true;
            }

            bool ownerChanged = state.OwnerProfileId == profileId;
            if (state.OwnerProfileId == profileId)
                state.OwnerProfileId = state.Members.Keys.First();
            DateTime now = DateTime.UtcNow;
            TouchLocked(state, now);
            PublishMemberRemoved(state, profileId, actorProfileId, now, recipients);
            if (ownerChanged)
                PublishPartyUpdated(state, recipients);
            return true;
        }
    }

    public bool ExtendExpiry(
        Guid partyId,
        Guid ownerProfileId,
        TimeSpan duration,
        out PartySnapshot? party)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (!parties.TryGetValue(partyId, out PartyState? state)
                || state.OwnerProfileId != ownerProfileId)
            {
                party = null;
                return false;
            }

            DateTime now = DateTime.UtcNow;
            state.ExpiresAt = now.Add(duration > TimeSpan.Zero ? duration : DefaultLifetime);
            TouchLocked(state, now);
            PublishExpiryRenewed(state);
            party = ToSnapshot(state);
            return true;
        }
    }

    public bool SetAutoRemove(Guid partyId, Guid profileId, bool enabled)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (!parties.TryGetValue(partyId, out PartyState? state)
                || !state.Members.ContainsKey(profileId))
                return false;

            if (enabled)
                state.AutoRemoveProfiles.Add(profileId);
            else
                state.AutoRemoveProfiles.Remove(profileId);
            TouchLocked(state, DateTime.UtcNow);
            return true;
        }
    }

    public void HandleDisconnect(Guid profileId)
    {
        Guid partyId;
        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (!partyByProfile.TryGetValue(profileId, out partyId)
                || !parties.TryGetValue(partyId, out PartyState? state)
                || !state.AutoRemoveProfiles.Contains(profileId))
                return;
        }

        Leave(partyId, profileId, profileId);
    }

    public bool Invite(Guid partyId, Guid ownerProfileId, Guid profileId)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (profileId == Guid.Empty
                || !parties.TryGetValue(partyId, out PartyState? state)
                || state.OwnerProfileId != ownerProfileId
                || state.Members.ContainsKey(profileId))
                return false;

            DateTime now = DateTime.UtcNow;
            state.Invitations[profileId] = new PartyInvitationState(
                ownerProfileId,
                now,
                now.Add(InvitationLifetime));
            TouchLocked(state, now);
            PublishInvitationSent(state, profileId, ownerProfileId, now);
            return true;
        }
    }

    public bool RemoveInvitation(Guid partyId, Guid actorProfileId, Guid profileId)
    {
        lock (stateLock)
        {
            CleanupExpiredLocked();
            if (!parties.TryGetValue(partyId, out PartyState? state)
                || (state.OwnerProfileId != actorProfileId && profileId != actorProfileId)
                || !state.Invitations.Remove(profileId))
                return false;

            DateTime now = DateTime.UtcNow;
            TouchLocked(state, now);
            PublishInvitationRemoved(
                state,
                profileId,
                now,
                profileId == actorProfileId);
            return true;
        }
    }

    private PartySnapshot? GetByProfileIdLocked(Guid profileId)
        => partyByProfile.TryGetValue(profileId, out Guid partyId)
            && parties.TryGetValue(partyId, out PartyState? state)
                ? ToSnapshot(state)
                : null;

    private void CleanupExpiredLocked()
    {
        DateTime now = DateTime.UtcNow;
        foreach (PartyState state in parties.Values.Where(value => value.ExpiresAt <= now).ToArray())
            RemovePartyLocked(state, [.. state.Members.Keys]);
        foreach (PartyState state in parties.Values)
        {
            foreach (Guid profileId in state.Invitations
                .Where(value => value.Value.ExpiresAt <= now)
                .Select(value => value.Key)
                .ToArray())
            {
                state.Invitations.Remove(profileId);
                PublishInvitationExpired(state, profileId, now);
            }

            foreach (string token in state.AccessTokens
                .Where(value => value.Value.ExpiresAt <= now)
                .Select(value => value.Key)
                .ToArray())
                state.AccessTokens.Remove(token);
        }
    }

    private void RemovePartyLocked(PartyState state, IReadOnlyCollection<Guid> recipients)
    {
        parties.Remove(state.PartyId);
        foreach (Guid profileId in state.Members.Keys)
            partyByProfile.Remove(profileId);
        Publish(
            state,
            "PARTY_PARTY_DELETED",
            CreatePartyIdentity(state),
            recipients);
    }

    private static void TouchLocked(PartyState state, DateTime now)
    {
        state.LastModifiedAt = now;
        state.Revision++;
    }

    private void PublishPartyCreated(PartyState state)
        => Publish(state, "PARTY_PARTY_CREATED", CreatePartyIdentity(state), state.Members.Keys);

    private void PublishPartyUpdated(PartyState state, IEnumerable<Guid> recipients)
    {
        JsonObject content = CreatePartyIdentity(state);
        content["ownerProfileId"] = state.OwnerProfileId;
        content["lastModifiedAt"] = state.LastModifiedAt;
        Publish(state, "PARTY_PARTY_UPDATED", content, recipients);
    }

    private void PublishExpiryRenewed(PartyState state)
    {
        JsonObject content = CreatePartyIdentity(state);
        content["expiresAt"] = state.ExpiresAt;
        Publish(state, "PARTY_PARTY_EXPIRY_RENEWED", content, state.Members.Keys);
    }

    private void PublishMemberAdded(PartyState state, Guid profileId, DateTime addedAt)
    {
        JsonObject content = CreatePartyIdentity(state);
        content["profileId"] = profileId;
        content["addedAt"] = addedAt;
        Publish(state, "PARTY_MEMBER_ADDED", content, state.Members.Keys);
    }

    private void PublishMemberRemoved(
        PartyState state,
        Guid removedProfileId,
        Guid removerProfileId,
        DateTime removedAt,
        IEnumerable<Guid> recipients)
    {
        JsonObject content = CreatePartyIdentity(state);
        content["removerProfileId"] = removerProfileId;
        content["removedProfileId"] = removedProfileId;
        content["removedAt"] = removedAt;
        Publish(state, "PARTY_MEMBER_REMOVED", content, recipients);
    }

    private void PublishInvitationSent(
        PartyState state,
        Guid inviteeProfileId,
        Guid inviterProfileId,
        DateTime createdAt)
    {
        JsonObject content = CreatePartyIdentity(state);
        content["inviteeProfileId"] = inviteeProfileId;
        content["inviterProfileId"] = inviterProfileId;
        content["createdAt"] = createdAt;
        content["expiresAt"] = createdAt.Add(InvitationLifetime);
        Publish(
            state,
            "PARTY_INVITATION_SENT",
            content,
            state.Members.Keys.Append(inviteeProfileId));
    }

    private void PublishInvitationRemoved(
        PartyState state,
        Guid inviteeProfileId,
        DateTime changedAt,
        bool declined)
    {
        JsonObject content = CreatePartyIdentity(state);
        content["inviteeProfileId"] = inviteeProfileId;
        content[declined ? "declinedAt" : "cancelledAt"] = changedAt;
        Publish(
            state,
            declined ? "PARTY_INVITATION_DECLINED" : "PARTY_INVITATION_CANCELLED",
            content,
            state.Members.Keys.Append(inviteeProfileId));
    }

    private void PublishInvitationExpired(
        PartyState state,
        Guid inviteeProfileId,
        DateTime expiredAt)
    {
        JsonObject content = CreatePartyIdentity(state);
        content["inviteeProfileId"] = inviteeProfileId;
        content["inviterProfileId"] = state.OwnerProfileId;
        content["expiresAt"] = expiredAt;
        content["expiredAt"] = expiredAt;
        Publish(
            state,
            "PARTY_INVITATION_EXPIRED",
            content,
            state.Members.Keys.Append(inviteeProfileId));
    }

    private void Publish(
        PartyState state,
        string notificationType,
        JsonObject content,
        IEnumerable<Guid> recipients)
        => notifications.Publish(
            recipients,
            new UbiServicesNotification(
                notificationType,
                new JsonObject
                {
                    ["spaceId"] = JdNextConstants.GameSpaceId,
                    ["content"] = content
                },
                DateTime.UtcNow,
                JdNextConstants.GameSpaceId));

    private static JsonObject CreatePartyIdentity(PartyState state)
        => new()
        {
            ["partyId"] = state.PartyId,
            ["groupId"] = JdNextConstants.PartyGroupId
        };

    private static PartySnapshot ToSnapshot(PartyState state)
        => new(
            state.PartyId,
            state.OwnerProfileId,
            state.CreatedAt,
            state.LastModifiedAt,
            state.ExpiresAt,
            state.Revision,
            [.. state.Members.Select(value => new PartyMemberSnapshot(
                value.Key,
                value.Value.CreatedAt,
                value.Value.LastModifiedAt,
                value.Value.Revision))],
            [.. state.Invitations.Select(value => new PartyInvitationSnapshot(
                value.Key,
                value.Value.InvitedByProfileId,
                value.Value.CreatedAt,
                value.Value.ExpiresAt))]);

    private static string HashToken(string token)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? string.Empty)));

    private sealed class PartyState
    {
        public required Guid PartyId { get; init; }
        public required Guid OwnerProfileId { get; set; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime LastModifiedAt { get; set; }
        public required DateTime ExpiresAt { get; set; }
        public required long Revision { get; set; }
        public Dictionary<Guid, PartyMemberState> Members { get; } = [];
        public Dictionary<Guid, PartyInvitationState> Invitations { get; } = [];
        public Dictionary<string, PartyTokenState> AccessTokens { get; } = [];
        public HashSet<Guid> AutoRemoveProfiles { get; } = [];
    }

    private sealed record PartyMemberState(DateTime CreatedAt, DateTime LastModifiedAt, long Revision);
    private sealed record PartyInvitationState(
        Guid InvitedByProfileId,
        DateTime CreatedAt,
        DateTime ExpiresAt);
    private sealed record PartyTokenState(Guid? IntendedProfileId, DateTime CreatedAt, DateTime ExpiresAt);
}
