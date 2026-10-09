using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.other.parties;

[ApiController]
[Route("v1/profiles/{profileId:guid}/parties/{partyId:guid}/autoRemoveOnDisconnect")]
public sealed class AutoRemoveOnDisconnect(
	IPartyService partyService,
	ISessionManager sessionManager) : ControllerBase
{
	[HttpPut]
	public IActionResult Put([FromRoute] Guid profileId, [FromRoute] Guid partyId)
	{
		if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			return Unauthorized();
		if (session.PlayerId != profileId || !partyService.SetAutoRemove(partyId, profileId, true))
			return StatusCode(StatusCodes.Status403Forbidden);
		return Ok(new
		{
			spaceId = JdNextConstants.GameSpaceId,
			applicationIds = UbisoftApplicationProfiles.GetApplicationIdsForTarget(
				JdNextConstants.GameSpaceId)
		});
	}
}
