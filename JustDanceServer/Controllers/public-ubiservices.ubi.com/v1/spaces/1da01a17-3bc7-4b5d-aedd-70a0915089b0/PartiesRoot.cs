using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/parties")]
public sealed class PartiesRoot(IPartyService partyService, ISessionManager sessionManager) : ControllerBase
{
	[HttpPost(Name = "PostParties")]
	public IActionResult PostParties([FromRoute] Guid spaceId, [FromBody] JsonElement body)
	{
		if (!body.TryGetProperty("ownerProfileId", out JsonElement ownerValue)
			|| !Guid.TryParse(ownerValue.GetString(), out Guid ownerProfileId))
			return BadRequest("ownerProfileId is required.");
		if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			return Unauthorized();
		if (session.PlayerId != ownerProfileId)
			return StatusCode(StatusCodes.Status403Forbidden);

		return Ok(PartyWireProjection.ToResponse(partyService.Create(ownerProfileId)));
	}
}
