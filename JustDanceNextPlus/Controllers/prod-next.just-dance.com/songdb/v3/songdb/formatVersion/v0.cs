using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.songdb.v3.songdb.formatVersion;

[ApiController]
[Route("songdb/v3/songdb/formatVersion/v0")]
public class V0(IMapService mapService) : ControllerBase
{
	[HttpGet]
	public IActionResult Get()
	{
		return Ok(mapService.SongDBTypeSet);
	}
}
