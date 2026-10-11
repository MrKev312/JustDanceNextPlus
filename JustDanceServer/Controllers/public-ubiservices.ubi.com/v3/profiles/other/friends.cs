using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.profiles.other;

[ApiController]
public sealed class Friends(IFriendService friendService, ISessionManager sessionManager) : ControllerBase
{
    [HttpGet("v2/profiles/me/friends")]
    public Task<IActionResult> GetLegacyFriends()
        => GetFriendsForSessionAsync();

    [HttpGet("v3/profiles/{profileId:guid}/friends")]
    public Task<IActionResult> GetFriends([FromRoute] Guid profileId)
        => GetFriendsForSessionAsync(profileId);

    [HttpPost("v2/profiles/me/friends")]
    public Task<IActionResult> InviteLegacyFriends([FromBody] FriendInviteRequest request)
        => InviteFriendsForSessionAsync(request);

    [HttpPost("v3/profiles/{profileId:guid}/friends")]
    public Task<IActionResult> InviteFriends(
        [FromRoute] Guid profileId,
        [FromBody] FriendInviteRequest request)
        => InviteFriendsForSessionAsync(request, profileId);

    [HttpPut("v2/profiles/me/friends/{friendProfileId:guid}")]
    public Task<IActionResult> AcceptLegacyFriend([FromRoute] Guid friendProfileId)
        => AcceptFriendForSessionAsync(friendProfileId);

    [HttpPut("v3/profiles/{profileId:guid}/friends/{friendProfileId:guid}")]
    public Task<IActionResult> AcceptFriend(
        [FromRoute] Guid profileId,
        [FromRoute] Guid friendProfileId)
        => AcceptFriendForSessionAsync(friendProfileId, profileId);

    [HttpDelete("v2/profiles/me/friends/{friendProfileId:guid}")]
    public Task<IActionResult> RemoveLegacyFriend([FromRoute] Guid friendProfileId)
        => RemoveFriendForSessionAsync(friendProfileId);

    [HttpDelete("v3/profiles/{profileId:guid}/friends/{friendProfileId:guid}")]
    public Task<IActionResult> RemoveFriend(
        [FromRoute] Guid profileId,
        [FromRoute] Guid friendProfileId)
        => RemoveFriendForSessionAsync(friendProfileId, profileId);

    private async Task<IActionResult> GetFriendsForSessionAsync(Guid? routeProfileId = null)
    {
        if (!TryGetAuthenticatedProfile(routeProfileId, out Guid profileId, out IActionResult? error))
            return error;

        IReadOnlyList<FriendRelationship> relationships =
            await friendService.GetRelationshipsAsync(profileId);
        return Ok(new
        {
            groupId = JdNextConstants.FriendsGroupId,
            friends = relationships.Select(relationship => new FriendResponse(
                relationship.ProfileId,
                relationship.State,
                false,
                string.Empty))
        });
    }

    private async Task<IActionResult> InviteFriendsForSessionAsync(
        FriendInviteRequest request,
        Guid? routeProfileId = null)
    {
        if (!TryGetAuthenticatedProfile(routeProfileId, out Guid profileId, out IActionResult? error))
            return error;

        foreach (Guid friendProfileId in request.Friends.Distinct())
        {
            if (!await friendService.SendInviteAsync(profileId, friendProfileId))
                return BadRequest($"Cannot invite profile '{friendProfileId}'.");
        }

        return Ok();
    }

    private async Task<IActionResult> AcceptFriendForSessionAsync(
        Guid friendProfileId,
        Guid? routeProfileId = null)
    {
        if (!TryGetAuthenticatedProfile(routeProfileId, out Guid profileId, out IActionResult? error))
            return error;

        return await friendService.AcceptInviteAsync(profileId, friendProfileId)
            ? Ok()
            : BadRequest($"Cannot accept profile '{friendProfileId}'.");
    }

    private async Task<IActionResult> RemoveFriendForSessionAsync(
        Guid friendProfileId,
        Guid? routeProfileId = null)
    {
        if (!TryGetAuthenticatedProfile(routeProfileId, out Guid profileId, out IActionResult? error))
            return error;

        return await friendService.RemoveRelationshipAsync(profileId, friendProfileId)
            ? Ok()
            : BadRequest($"Cannot remove profile '{friendProfileId}'.");
    }

    private bool TryGetAuthenticatedProfile(
        Guid? routeProfileId,
        out Guid profileId,
        out IActionResult error)
    {
        profileId = Guid.Empty;
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
        {
            error = Unauthorized();
            return false;
        }

        if (routeProfileId.HasValue && routeProfileId.Value != session.PlayerId)
        {
            error = StatusCode(StatusCodes.Status403Forbidden);
            return false;
        }

        profileId = session.PlayerId;
        error = null!;
        return true;
    }
}

public sealed class FriendInviteRequest
{
    public IReadOnlyList<Guid> Friends { get; init; } = [];
}

public sealed record FriendResponse(
    Guid Pid,
    string State,
    bool IsFavorite,
    string Nickname);
