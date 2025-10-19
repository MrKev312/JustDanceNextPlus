using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/parties")]
public class PartiesRoot(ITimingService timingService, IPartyManager partyManager) : ControllerBase
{
	[HttpPost(Name = "PostParties")]
	public IActionResult PostParties([FromBody] JsonElement body)
	{
		// Get the ownerProfileId
		string ownerProfileId = body.GetProperty("ownerProfileId").GetString()!;
		Guid ownerGuid = Guid.Parse(ownerProfileId);

		Guid partyId = partyManager.GenerateParty(ownerGuid);

		DateTime created = DateTime.UtcNow;
		DateTime lastModified = DateTime.UtcNow;
		DateTime expire = DateTime.UtcNow.AddMinutes(30);

		string response = $$"""
			{
				"partyId": "{{partyId}}",
				"partyTypeId": "514fad0b-5c7e-4e0e-93e1-5087ccd5fb09",
				"ownerProfileId": "{{ownerProfileId}}",
				"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
				"groupId": "d8e7e2fc-3d99-4d39-8df8-e34c53630f9a",
				"obj": {},
				"createdAt": "{{timingService.TimeString(created)}}",
				"lastModifiedAt": "{{timingService.TimeString(lastModified)}}",
				"expiresAt": "{{timingService.TimeString(expire)}}",
				"lockState": "unlocked",
				"revision": "123466d6",
				"crossPlayEnabled": true,
				"restrictedPlatforms": ["NX", "PC", "ORBIS", "PS5", "DURANGO", "XBX", "XboxScarlett"]
			}
			""";

		return Content(response, "application/json");
	}
}
