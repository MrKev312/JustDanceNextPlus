using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.localizations.strings;

[ApiController]
[Route("v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/localizations/strings/all")]
public class All(TimingService timingService) : ControllerBase
{
	[HttpGet(Name = "GetAll")]
	public IActionResult GetAll()
	{
		// Get time over an hour from now
		DateTime expiresAt = DateTime.UtcNow.AddHours(1);

		string response = $$"""
			{
				"getUrl": "https://prod-next.just-dance.com/jsons/All.en-US.json.gz",
				"getUrlExpiresAt": "{{timingService.TimeString(expiresAt)}}",
				"localeCode": "en-US",
				"md5": "d7dab8accbbaef4b109de1dbd1c9f091",
				"revision": 1,
				"spaceRevision": 121
			}
			""";

		return Content(response, "application/json");
	}
}
