using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.leaderboard.v1.map.mapid.ghostmode;

[ApiController]
[Route("leaderboard/v1/map/{mapId:guid}/ghostmode/opponent")]
public class Opponent(IUserDataService userDataService, ISessionManager sessionManager) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> Post([FromRoute] Guid mapId)
	{
		// Grab the session token from the request
		string? sessionToken = Request.Headers["ubi-sessionid"].ToString();

		// If the session token is null or empty, return a 401
		if (string.IsNullOrEmpty(sessionToken))
			return Unauthorized();

		// Get a guid from the session token
		Guid sessionGuid = Guid.Parse(sessionToken);

		// Get the user profile from the session token
		if (sessionManager.TryGetSessionById(sessionGuid, out Session? session) == false)
			return Unauthorized();

		Guid userProfile = session.PlayerId;

		// Get the leaderboard for the mapId
		Leaderboard leaderboard = await userDataService.GetRandomOpponentAsync(mapId, userProfile);

		// Return the leaderboard
		return Ok(leaderboard);
	}
}
