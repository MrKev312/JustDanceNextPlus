using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.other.inventory;

[ApiController]
[Route("v1/profiles/{guid:guid}/inventory/rulesexecution")]
public class RulesExecution : ControllerBase
{
	[HttpPost]
	public IActionResult PostRuleExecution([FromBody] JsonElement body)
	{
		string response = """
			{
				"executedRules": [],
				"remainingRulesCount": 0,
				"failedExecution": null
			}
			""";
		return Content(response, "application/json");
	}
}
