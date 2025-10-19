using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.songdb;

[ApiController]
[Route("songdb/songDB")]
public class SongDB(IMapService mapService, JsonSettingsService jsonSettingsService) : ControllerBase
{
	[HttpGet(Name = "GetSongDB")]
	public IActionResult GetSongDB()
	{
		string response = JsonSerializer.Serialize(mapService.Songs, jsonSettingsService.PrettyPascalFormat);

		// Disable compression for this specific response
		Response.Headers.ContentEncoding = "identity";

		return Content(response, "application/json");
	}
}
