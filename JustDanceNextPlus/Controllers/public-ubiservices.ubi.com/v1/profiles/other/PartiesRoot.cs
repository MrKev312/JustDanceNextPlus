using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.other;

[ApiController]
[Route("v1/profiles/{guid:guid}/parties")]
public class PartiesRoot(ITimingService timingService, IPartyManager partyManager) : ControllerBase
{
	[HttpGet(Name = "GetParties")]
	public IActionResult GetParties([FromRoute] Guid guid)
	{
		// Get the party from the party manager by using the GUID from the route
		Party? party = partyManager.GetPartyByProfileId(guid);

		if (party == null)
		{
			// Create a new party
			partyManager.GenerateParty(guid);

			// Return empty json response
			return Content("""
				{
					"parties": []
				}
				""", "application/json");
		}

		DateTime created = DateTime.Now.AddHours(-5);
		DateTime lastModified = DateTime.Now.AddHours(-4);
		DateTime expiresAt = DateTime.UtcNow.AddHours(5);

		string response = $$"""
			{
				"parties": [{
					"partyId": "{{party.PartyId}}",
					"partyTypeId": "514fad0b-5c7e-4e0e-93e1-5087ccd5fb09",
					"ownerProfileId": "{{party.PartyMembers[0]}}",
					"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
					"groupId": "d8e7e2fc-3d99-4d39-8df8-e34c53630f9a",
					"obj": {},
					"createdAt": "{{timingService.TimeString(created)}}",
					"lastModifiedAt": "{{timingService.TimeString(lastModified)}}",
					"expiresAt": "{{timingService.TimeString(expiresAt)}}",
					"lockState": "unlocked",
					"revision": "123466d6",
					"crossPlayEnabled": true,
					"restrictedPlatforms": ["NX", "PC", "ORBIS", "PS5", "DURANGO", "XBX", "XboxScarlett"]
				}]
			}
			""";

		return Content(response, "application/json");
	}
}
