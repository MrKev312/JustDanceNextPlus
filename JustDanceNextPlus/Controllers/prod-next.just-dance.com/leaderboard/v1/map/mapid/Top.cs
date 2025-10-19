using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.leaderboard.v1.map.mapid;

[ApiController]
[Route("leaderboard/v1/map/{mapId:guid}/top")]
public class Top(IUserDataService userDataService) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> Get([FromRoute] Guid mapId, [FromQuery] int limit = 10, [FromQuery] int skip = 0)
	{
		// Get the leaderboard for the mapId
		Leaderboard leaderboard = await userDataService.GetLeaderboardByMapIdAsync(mapId, limit, skip);

		// Return the leaderboard
		return Ok(leaderboard);
	}
}
