﻿using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/items")]
public class Items : ControllerBase
{
	[HttpGet]
	public IActionResult GetItems()
	{
		string response = """
			{
				"items": []
			}
			""";

		return Content(response, "application/json");
	}
}
