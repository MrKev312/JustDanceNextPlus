using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.parties;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/parties/{partyId:guid}/expiry")]
public sealed class Expiry(IPartyService partyService, ISessionManager sessionManager) : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromRoute] Guid spaceId, [FromRoute] Guid partyId)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();
        return partyService.ExtendExpiry(
            partyId,
            session.PlayerId,
            TimeSpan.FromMinutes(30),
            out PartySnapshot? party)
            ? Ok(new { expiresAt = party!.ExpiresAt, revision = party.Revision.ToString() })
            : StatusCode(StatusCodes.Status403Forbidden);
    }
}
