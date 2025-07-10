using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.leaderboard.playlist.playlistid;

[ApiController]
[Route("leaderboard/v1/playlist/{playlistId:guid}/around")]
public class Around(UserDataService userDataService, SessionManager sessionManager) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> Get([FromRoute] Guid playlistId, [FromQuery] int limit = 3)
	{
		// Get the session id
		string? sessionIdString = Request.Headers["ubi-sessionid"];

		if (sessionIdString == null)
			return Unauthorized();

		Guid sessionId = Guid.Parse(sessionIdString);

		Session? session = sessionManager.GetSessionById(sessionId);

		if (session == null)
			return Unauthorized();

		// Get the leaderboard for the mapId
		Leaderboard leaderboard = await userDataService.GetPlaylistLeaderboardAroundAsync(playlistId, session.PlayerId, limit);

		// Return the leaderboard
		return Ok(leaderboard);
	}
}
