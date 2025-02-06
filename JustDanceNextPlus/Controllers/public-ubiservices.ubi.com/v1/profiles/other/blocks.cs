using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.other;

[ApiController]
[Route("v1/profiles/{guid:guid}/blocks")]
public class Blocks : ControllerBase
{
	[HttpGet(Name = "GetBlocks")]
	public IActionResult GetBlocks([FromRoute] Guid guid)
	{
		string response = """
			{
				"groupId": "",
				"profiles": []
			}
			""";

		return Content(response, "application/json");
	}
}
