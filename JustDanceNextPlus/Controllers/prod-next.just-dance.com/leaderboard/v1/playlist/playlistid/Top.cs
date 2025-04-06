using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.leaderboard.v1.playlist.playlistid;

[ApiController]
[Route("leaderboard/v1/map/{playlistId:guid}/top")]
public class Top(UserDataService userDataService) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> Get([FromRoute] Guid playlistId, [FromQuery] int limit = 10, [FromQuery] int skip = 0)
	{
		// Get the leaderboard for the mapId
		Leaderboard leaderboard = await userDataService.GetPlaylistLeaderboardByPlaylistIdAsync(playlistId, limit, skip);

		// Return the leaderboard
		return Ok(leaderboard);
	}
}
