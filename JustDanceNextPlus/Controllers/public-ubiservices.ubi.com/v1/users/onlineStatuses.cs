using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.users;

[ApiController]
[Route("v1/users/onlineStatuses")]
public class OnlineStatuses() : ControllerBase
{
	[HttpGet(Name = "GetOnlineStatuses")]
	public IActionResult GetOnlineStatuses()
	{
		string[] userIds = Request.Query["userIds"].ToString().Split(",");

		if (userIds.Length == 0)
		{
			return BadRequest(new { message = "Missing userIds" });
		}

		if (userIds.Length == 1)
		{
			string response = $$"""
				{
					"onlineStatuses": [{
						"connections": [{
							"applicationId": "77b6223b-eb41-4ccd-833c-c7b16537fde3",
							"connectionProfileId": "{{userIds[0]}}",
							"createdAt": "2024-10-04T07:56:49.4422359Z",
							"lastModifiedAt": "2024-10-04T07:56:49.4422359Z",
							"stagingSpaceId": ""
						}],
						"manuallySet": true,
						"onlineStatus": "away",
						"userId": "{{userIds[0]}}"
					}]
				}
				""";

			return Content(response, "application/json");
		}

		List<object> onlineStatuses = [];

		foreach (string userId in userIds)
		{
			onlineStatuses.Add(new
			{
				connections = Array.Empty<string>(),
				onlineStatus = "offline",
				userId
			});
		}

		var responseObj = new
		{
			onlineStatuses
		};

		return Ok(responseObj);

	}
}
