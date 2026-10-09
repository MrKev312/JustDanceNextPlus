using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.songdb.v2;

[ApiController]
[Route("songdb/v2/database")]
public class Database(IModernJduSongDatabaseService songDatabaseService) : ControllerBase
{
    [HttpGet]
	public IActionResult GetDatabase([FromQuery] string? assetPlatform)
    {
		string? normalizedPlatform = JduAssetPlatform.Normalize(assetPlatform);
		return normalizedPlatform is null
			? BadRequest("assetPlatform does not identify a supported JDU platform.")
			: File(songDatabaseService.SerializeDatabase(normalizedPlatform), "application/octet-stream");
    }
}
