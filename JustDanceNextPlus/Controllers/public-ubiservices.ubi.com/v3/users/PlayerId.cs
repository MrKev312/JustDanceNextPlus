using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v3.users;

[ApiController]
[Route("v3/users/{guid:guid}")]
public class PlayerId : ControllerBase
{
	// Get: /public-ubiservices.ubi.com/v3/users/{guid}
	// Simply log the request and return an empty json response
	[HttpGet(Name = "GetUserLanguages")]
	public IActionResult GetUserLanguages([FromRoute] Guid guid)
	{
		string response = """
			{
				"country": "NL",
				"preferredLanguage": "en"
			}
			""";

		return Content(response, "application/json");
	}
}
