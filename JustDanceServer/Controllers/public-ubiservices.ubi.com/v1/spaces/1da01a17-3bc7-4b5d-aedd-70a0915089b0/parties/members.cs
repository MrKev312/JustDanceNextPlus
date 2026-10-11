using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.parties;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/parties/{partyId:guid}/members")]
public sealed class Members(IPartyService partyService, ISessionManager sessionManager) : ControllerBase
{
    [HttpGet(Name = "GetMembers")]
    public IActionResult GetMembers([FromRoute] Guid spaceId, [FromRoute] Guid partyId)
    {
        PartySnapshot? party = partyService.Get(partyId);
        if (party is null)
            return NotFound();

        return Ok(new
        {
            ownerProfileId = party.OwnerProfileId,
            members = party.Members.Select(member => new PartyMemberResponse
            {
                ProfileId = member.ProfileId,
                AddedAt = member.CreatedAt,
                LastModifiedAt = member.LastModifiedAt,
                Revision = member.Revision
            })
        });
    }

    [HttpPost]
    public IActionResult Join(
        [FromRoute] Guid spaceId,
        [FromRoute] Guid partyId,
        [FromBody] JoinPartyRequest request)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();
        return partyService.Join(partyId, session.PlayerId, request.AccessToken)
            ? Ok()
            : BadRequest("The party access token is invalid or expired.");
    }

    [HttpDelete("{profileId:guid}", Name = "DeleteMember")]
    public IActionResult DeleteMember(
        [FromRoute] Guid spaceId,
        [FromRoute] Guid partyId,
        [FromRoute] Guid profileId)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();
        return partyService.Leave(partyId, profileId, session.PlayerId) ? Ok() : StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record JoinPartyRequest(string AccessToken);
