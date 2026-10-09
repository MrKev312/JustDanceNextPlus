using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.other;

[ApiController]
[Route("v1/profiles/{profileId:guid}/parties")]
public sealed class PartiesRoot(IPartyService partyService, ISessionManager sessionManager) : ControllerBase
{
	[HttpGet(Name = "GetParties")]
	public IActionResult GetParties([FromRoute] Guid profileId)
	{
		if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			return Unauthorized();
		if (session.PlayerId != profileId)
			return StatusCode(StatusCodes.Status403Forbidden);

		PartySnapshot? party = partyService.GetByProfileId(profileId);
		return Ok(new
		{
			parties = party is null
				? Array.Empty<PartyResponse>()
				: [PartyWireProjection.ToResponse(party)]
		});
	}
}
