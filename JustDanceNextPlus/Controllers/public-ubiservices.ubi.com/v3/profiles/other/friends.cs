using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v3.profiles.other;

[ApiController]
[Route("v3/profiles/{guid:guid}/friends")]
public class Friends : ControllerBase
{
	[HttpGet(Name = "GetFriends")]
	public IActionResult GetFriends([FromRoute] Guid guid)
	{
		string response = """
			{
				"groupId": "5728d8ee-1e66-424b-b183-1d2ea5717fb4",
				"friends": []
			}
			""";

		return Content(response, "application/json");
	}
}
