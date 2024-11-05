using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v2.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.battlepasses;

[ApiController]
[Route("v2/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/battlepasses/seasons")]
public class Seasons : ControllerBase
{
	[HttpGet]
	public IActionResult GetSeasons()
	{
		string response = """
			{
				"seasons": []
			}
			""";
		return Content(response, "application/json");
	}
}
