using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v3.profiles.users;

[ApiController]
[Route("v3/profiles/users/{guid:guid}")]
public class PlayerId : ControllerBase
{
	[HttpGet(Name = "GetUsers")]
	public IActionResult GetUsers([FromRoute] Guid guid)
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
