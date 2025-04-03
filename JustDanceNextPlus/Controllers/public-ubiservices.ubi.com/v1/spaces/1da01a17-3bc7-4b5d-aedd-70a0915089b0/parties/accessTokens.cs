using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.parties;

[ApiController]
[Route("v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/parties/{guid:guid}/accessTokens")]
public class AccessTokens(TimingService timingService) : ControllerBase
{
	[HttpPost]
	public IActionResult Post([FromRoute] Guid guid)
	{
		DateTime created = DateTime.UtcNow;

		byte[] tokenData = new byte[20];
		Random.Shared.NextBytes(tokenData);
		string accessToken = Convert.ToHexStringLower(tokenData);

		string response = $$"""
			{
				"accessToken": "{{accessToken}}",
				"createdAt": "{{timingService.TimeString(created)}}",
				"expiresAt": null
			}
			""";

		return Content(response, "application/json");
	}
}
