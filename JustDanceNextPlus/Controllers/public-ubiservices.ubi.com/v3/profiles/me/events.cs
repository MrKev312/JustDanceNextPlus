using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v3.profiles.me;

[ApiController]
[Route("v3/profiles/me/events")]
public class Events : ControllerBase
{
	[HttpPost(Name = "PostEvents")]
	public IActionResult PostEvents([FromBody] object body)
	{
		return Content("{}", "application/json");
	}
}
