using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.playlistdb.v2.playlistdb.formatVersion;

[ApiController]
[Route("playlistdb/v2/playlistdb/formatVersion/v0")]
public class PlaylistDBController(PlaylistService playlistService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetFormatVersion()
	{
		return Ok(playlistService.PlaylistDB);
	}
}
