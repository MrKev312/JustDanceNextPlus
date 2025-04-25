using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.leaderboard.playlist.playlistid;

[ApiController]
[Route("leaderboard/v1/playlist/{playlistId:guid}/users")]
public class Users(UserDataService userDataService) : ControllerBase
{
	[HttpPost]
	public IActionResult Post([FromRoute] Guid playlistId, [FromBody] RequestBody userIds)
	{
		// Get the profiles of the users
		Leaderboard leaderboard = userDataService.GetPlaylistLeaderboardFromIdsAsync(playlistId, userIds.Users).Result;

		// Return the profiles
		return Ok(leaderboard);
	}

	public class RequestBody
	{
		[JsonPropertyName("users")]
		public List<Guid> Users { get; set; } = [];
	}
}
