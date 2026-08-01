using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.songdb.v3;

[ApiController]
[Route("songdb/v3/songpacks")]
public class Songpacks(IMapService mapService) : ControllerBase
{
	[HttpGet]
	public IActionResult Get()
	{
		Dictionary<string, SongPackEntry> songPacks = mapService.SongDBTypeSet.SongOffers.Claims
			.Where(claim => claim.Value.SongIds.Count > 0)
			.ToDictionary(
				claim => claim.Key,
				claim => new SongPackEntry
				{
					Name = claim.Key,
					SongIds = [.. claim.Value.SongIds.OrderBy(songId => songId).Select(songId => songId.ToString())]
				});

		return Ok(new { SongPacks = songPacks });
	}

	public class SongPackEntry
	{
		public required string Name { get; init; }
		public required IReadOnlyList<string> SongIds { get; init; }
	}
}
