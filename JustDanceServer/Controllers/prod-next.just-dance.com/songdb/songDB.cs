using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.songdb;

[ApiController]
[Route("songdb/songDB")]
public class SongDB(IMapService mapService, JsonSettingsService jsonSettingsService) : ControllerBase
{
	[HttpGet(Name = "GetSongDB")]
	public IActionResult GetSongDB()
	{
		string platform = ClientPlatform.FromRequest(Request);
		string response = JsonSerializer.Serialize(mapService.GetSongs(platform), jsonSettingsService.PrettyPascalFormat);

		// Disable compression for this specific response
		Response.Headers.ContentEncoding = "identity";

		return Content(response, "application/json");
	}
}
