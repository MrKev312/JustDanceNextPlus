using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.playlistdb.v2.playlistdb.formatVersion;

[ApiController]
[Route("playlistdb/v2/playlistdb/formatVersion/v0")]
public class V0(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet]
	public IActionResult GetPlaylistDb()
	{
		return Ok(System.IO.File.ReadAllText(Path.Combine(pathSettings.Value.JsonsPath, "playlistdb.json")));
	}
}
