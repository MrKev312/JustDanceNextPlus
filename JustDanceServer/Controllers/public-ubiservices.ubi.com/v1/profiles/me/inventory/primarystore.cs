using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me.inventory;

[ApiController]
[Route("v1/profiles/me/inventory/primarystore")]
public class PrimaryStore : ControllerBase
{
	[HttpPost]
	public IActionResult PostPrimaryStore([FromBody] JsonElement body)
	{
		string response = """
			{
				"results": [],
				"errors": [],
				"additionalDetails": []
			}
			""";

		return Content(response, "application/json");
	}
}
