using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces;

[ApiController]
[Route("v1/spaces/news")]
public sealed class News(IEditorialService editorialService) : ControllerBase
{
	[HttpGet(Name = "GetNews")]
	public IActionResult GetNews()
	{
		EditorialAudience audience = EditorialAudienceResolver.FromRequest(Request);
		Guid spaceId = Guid.TryParse(Request.Query["spaceId"].FirstOrDefault(), out Guid requested)
			? requested
			: JdNextConstants.GameSpaceId;
		return Ok(new
		{
			news = editorialService.GetEntries(audience)
				.Select(entry => NewsWireProjection.ToResponse(entry, spaceId))
		});
	}
}
