using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.parties;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/parties/{partyId:guid}/invitations")]
public sealed class Invitations(IPartyService partyService, ISessionManager sessionManager) : ControllerBase
{
    [HttpGet(Name = "GetInvitations")]
    public IActionResult GetInvitations([FromRoute] Guid spaceId, [FromRoute] Guid partyId)
    {
        PartySnapshot? party = partyService.Get(partyId);
        return party is null ? NotFound() : Ok(new { invitations = party.Invitations });
    }

    [HttpPost]
    public IActionResult Invite(
        [FromRoute] Guid spaceId,
        [FromRoute] Guid partyId,
        [FromBody] PartyInvitationRequest request)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();
        return partyService.Invite(partyId, session.PlayerId, request.ProfileId) ? Ok() : StatusCode(StatusCodes.Status403Forbidden);
    }

    [HttpDelete("{profileId:guid}")]
    public IActionResult Remove(
        [FromRoute] Guid spaceId,
        [FromRoute] Guid partyId,
        [FromRoute] Guid profileId)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();
        return partyService.RemoveInvitation(partyId, session.PlayerId, profileId) ? Ok() : StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record PartyInvitationRequest(Guid ProfileId);
