using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.me.populations;

[ApiController]
[Route("v1/profiles/me/populations/data")]
public class Data : ControllerBase
{
	[HttpPut]
	public IActionResult PutPopulationsData([FromBody] JsonElement body)
	{
		// Get the spaceId from the request body
		string? spaceId = body.GetProperty("spaceId").GetString();

		if (spaceId == null)
		{
			return BadRequest("Missing 'spaceId' field in request body");
		}

		string response;

		if (spaceId == "df67242d-9616-4652-9f40-fbcfde99b3c6")
		{
			response = """
				{
					"populations": [{
						"name": "prod",
						"obj": {},
						"subject": "us_staging",
						"spaceId": "df67242d-9616-4652-9f40-fbcfde99b3c6",
						"assignmentTime": "2024-10-01T09:29:46Z"
					}, {
						"name": "NoRemoteLog",
						"obj": {},
						"subject": "remotelogs",
						"spaceId": "df67242d-9616-4652-9f40-fbcfde99b3c6",
						"assignmentTime": "2024-10-03T20:06:27Z"
					}]
				}
				""";
		}
		else if (spaceId == "1da01a17-3bc7-4b5d-aedd-70a0915089b0")
		{
			response = """
				{
					"populations": [{
						"name": "Switch",
						"obj": {},
						"subject": "Platform",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-06-14T13:32:40Z"
					}, {
						"name": "CTR",
						"obj": {},
						"subject": "ABTestCategoriesAlgorithm",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-05-30T13:29:32Z"
					}, {
						"name": "ALSComplexTuned",
						"obj": {},
						"subject": "ABTestCategoriesALS",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-05-30T13:29:32Z"
					}, {
						"name": "DetailedDancemoveTrackingDisabled",
						"obj": {},
						"subject": "DetailedDancemoveTracking",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-10-03T20:06:27Z"
					}, {
						"name": "DetailedQualityStreamTrackingDisabled",
						"obj": {},
						"subject": "DetailedQualityStreamTracking",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-10-03T20:06:27Z"
					}, {
						"name": "Public",
						"obj": {},
						"subject": "Endpoint",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-05-30T13:29:32Z"
					}, {
						"name": "Errors",
						"obj": {},
						"subject": "RemoteLogs",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-10-03T20:06:27Z"
					}, {
						"name": "SkeletonTrackingDisabled",
						"obj": {},
						"subject": "SkeletonTracking",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-10-03T20:06:27Z"
					}, {
						"name": "NoTests",
						"obj": {},
						"subject": "SSLPinningCanary",
						"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
						"assignmentTime": "2024-10-03T20:06:27Z"
					}]
				}
				""";
		}
		else
		{
			return BadRequest("Invalid 'spaceId' field in request body");
		}

		return Content(response, "application/json");
	}
}
