using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.songdb.v3.downloadable_songs_metadata.formatVersion;

[ApiController]
[Route("songdb/v3/downloadable-songs-metadata/formatVersion/v0")]
public class V0(IMapService mapService) : ControllerBase
{
	[HttpGet]
	public IActionResult Get()
	{
		return Ok(new
		{
			mapService.AssetMetadataPerSong
		});
	}
}
