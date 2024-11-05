using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v4.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.configs;

[ApiController]
[Route("v4/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/configs/events")]
public class Events : ControllerBase
{
	[HttpGet]
	public IActionResult GetEvents()
	{
		string response = """
			{}
			""";

		return Content(response, "application/json");
	}
}
