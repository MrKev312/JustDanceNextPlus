﻿using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/live-tile")]
public class LiveTile : ControllerBase
{
	[HttpGet(Name = "GetLiveTile")]
	public IActionResult GetLiveTile()
	{
		string response = """
			{
				"4f598326-72d4-483d-be62-a7623746a1b4": {
					"buttonId": 489,
					"subtitleId": 5163,
					"deepLink": "//jdn?sid=Details&pid=PlaylistDetailsPage_LandscapeCursor&PlaylistId=c4cdeeee-da41-4193-aa5c-f83dc5c6c57b",
					"priority": 2,
					"assets": {
						"backgroundImage": "https://prod-next.just-dance.com/main_menu/live-tile.bundle",
					}
				}
			}
			""";

		return Content(response, "application/json");
	}
}
