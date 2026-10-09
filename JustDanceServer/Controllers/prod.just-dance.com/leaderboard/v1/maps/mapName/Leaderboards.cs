using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.leaderboard.v1.maps.mapName;

[ApiController]
[Route("leaderboard/v1/maps/{mapName}")]
public sealed class Leaderboards(ILeaderboardService leaderboardService) : ControllerBase
{
	[HttpGet("world")]
	public async Task<IActionResult> GetWorld([FromRoute] string mapName, [FromQuery] int count = 5)
		=> Ok(ToResponse(await leaderboardService.GetDetailedLeaderboardAsync(mapName, count)));

	[HttpGet("countries/{countryId:int}")]
	public async Task<IActionResult> GetCountry(
		[FromRoute] string mapName,
		[FromRoute] int countryId,
		[FromQuery] int count = 5)
		=> Ok(ToResponse(await leaderboardService.GetDetailedLeaderboardAsync(
			mapName,
			count,
			countryId)));

	[HttpPost("friends")]
	public async Task<IActionResult> GetFriends(
		[FromRoute] string mapName,
		[FromBody] Guid[] profileIds,
		[FromQuery] int count = 5)
		=> Ok(ToResponse(await leaderboardService.GetDetailedLeaderboardAsync(
			mapName,
			count,
			profileIds: profileIds)));

	internal static ClassicLeaderboardResponse ToResponse(LeaderboardPage page)
		=> new()
		{
			PlayerCount = page.PlayerCount,
			Entries = [.. page.Entries.Select(entry => new ClassicLeaderboardEntry
			{
				Name = entry.Name,
				Country = entry.CountryId,
				Avatar = entry.AvatarId,
				Score = entry.Score,
				Rank = entry.Rank,
				JdPoints = entry.JdPoints,
				Alias = entry.AliasId,
				AliasGender = entry.AliasGender,
				PortraitBorder = entry.PortraitBorderId,
				ProfileId = entry.ProfileId,
				PlatformId = entry.PlatformId
			})]
		};
}
