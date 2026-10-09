using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.profile.v2;

[ApiController]
[Route("profile/v2/country")]
public sealed class Country(
	IPlayerMetadataService metadataService,
	ISessionManager sessionManager) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> GetCountry()
	{
		if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			return Unauthorized();
		return Ok(new { country = (await metadataService.GetAsync(session.PlayerId)).CountryCode });
	}
}
