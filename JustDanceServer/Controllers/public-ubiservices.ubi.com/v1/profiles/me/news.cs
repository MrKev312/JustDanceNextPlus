using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Configuration;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me;

[ApiController]
[Route("v1/profiles/me/news")]
public sealed class News(IEditorialService editorialService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetNews([FromQuery] Guid? spaceId)
	{
		EditorialAudience audience = EditorialAudienceResolver.FromRequest(Request);
		Guid responseSpaceId = spaceId ?? GetRequestSpaceId() ?? Guid.Empty;
		return Ok(new
		{
			news = editorialService.GetEntries(audience)
				.Select(entry => NewsWireProjection.ToResponse(entry, responseSpaceId))
		});
	}

	private Guid? GetRequestSpaceId()
	{
		return Guid.TryParse(Request.Headers["Ubi-AppId"].FirstOrDefault(), out Guid applicationId)
			&& UbisoftApplicationProfiles.TryGetSessionIdentity(
				applicationId,
				out UbisoftSessionIdentity? identity)
				? identity.SpaceId
				: null;
	}
}
