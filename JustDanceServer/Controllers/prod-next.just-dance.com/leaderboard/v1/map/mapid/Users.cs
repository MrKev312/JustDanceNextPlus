using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.leaderboard.v1.map.mapid;

[ApiController]
[Route("leaderboard/v1/map/{mapId:guid}/users")]
public class Users(IUserDataService userDataService, IMapService mapService) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> Post([FromRoute] Guid mapId, [FromBody] RequestBody userIds)
	{
		if (!mapService.TryGetMapName(mapId, out string mapName))
			return NotFound();

		Leaderboard leaderboard = await userDataService.GetLeaderboardFromIdsAsync(mapName, userIds.Users);

		// Return the profiles
		return Ok(leaderboard);
	}

	public class RequestBody
	{
		[JsonPropertyName("users")]
		public List<Guid> Users { get; set; } = [];
	}
}
