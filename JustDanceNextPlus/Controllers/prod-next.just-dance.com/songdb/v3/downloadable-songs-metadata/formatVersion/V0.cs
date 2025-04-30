﻿using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.songdb.v3.downloadable_songs_metadata.formatVersion;

[ApiController]
[Route("songdb/v3/songdb/formatVersion/v0")]
public class V0(MapService mapService) : ControllerBase
{
	[HttpGet(Name = "GetSongdbFormatVersion")]
	public IActionResult GetSongdbFormatVersion()
	{
		return Ok(new
		{
			mapService.SongDB.AssetMetadataPerSong
		});
	}
}
