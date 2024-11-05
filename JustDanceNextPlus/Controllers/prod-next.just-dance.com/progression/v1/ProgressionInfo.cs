using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1;

[ApiController]
[Route("progression/v1/progression-info")]
public class ProgressionInfo(SessionManager sessionManager, UserDataService userDataService) : ControllerBase
{
	[HttpGet(Name = "GetProgressionInfo")]
	public IActionResult GetProgressionInfo()
	{
		// Get the session id from the request headers
		string? sessionId = Request.Headers["ubi-sessionid"];

		// If the session id is null or empty, return 401 Unauthorized
		if (string.IsNullOrEmpty(sessionId))
			return Unauthorized();

		Guid sessionGuid = Guid.Parse(sessionId);

		Session? session = sessionManager.GetSessionById(sessionGuid);

		// If the session is null, return 401 Unauthorized
		if (session == null)
			return Unauthorized();

		Profile? profile = userDataService.GetProfileByIdAsync(session.PlayerId).Result;

		return Ok(profile);
	}
}
