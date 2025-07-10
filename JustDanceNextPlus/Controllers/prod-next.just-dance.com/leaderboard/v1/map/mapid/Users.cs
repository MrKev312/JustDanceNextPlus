using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.leaderboard.v1.map.mapid;

[ApiController]
[Route("leaderboard/v1/map/{mapId:guid}/users")]
public class Users(UserDataService userDataService) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> Post([FromRoute] Guid mapId, [FromBody] RequestBody userIds)
	{
		// Get the profiles of the users
		Leaderboard leaderboard = await userDataService.GetLeaderboardFromIdsAsync(mapId, userIds.Users);

		// Return the profiles
		return Ok(leaderboard);
	}

	public class RequestBody
	{
		[JsonPropertyName("users")]
		public List<Guid> Users { get; set; } = [];
	}
}
