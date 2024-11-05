using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.me;

[ApiController]
[Route("v1/profiles/me/remotelog")]
public class RemoteLog : ControllerBase
{
	[HttpPost]
	public IActionResult PostRemoteLog([FromBody] JsonElement body)
	{
		string response = $$"""
			{
				"transactionId": "{{Guid.NewGuid()}}",
				"buildVersion": "1.10.6",
				"databaseConnection": true,
				"databaseReadWrite": true,
				"dependenciesCheckSuccess": true,
				"weakDependenciesCheckSuccess": true,
				"strongDependenciesCheckSuccess": true,
				"environment": "prod",
				"hostName": "01dfb37e102f",
				"hostIP": "172.17.0.31",
				"serviceName": "remotelogs",
				"colour": "green",
				"release_version": "1.10.6+RELEASE.6",
				"config_version": "1.11.23",
				"status": "OK",
				"statusMessage": "Success",
				"timeMilliseconds": 0
			}
			""";

		return Content(response, "application/json");
	}
}
