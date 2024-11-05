using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.other.parties;

[ApiController]
[Route("v1/profiles/{profileId:guid}/parties/{partyId:guid}/autoRemoveOnDisconnect")]
public class AutoRemoveOnDisconnect : ControllerBase
{
	[HttpPut]
	public IActionResult Put()
	{
		string response = """
			{
				"spaceId": "1da01a17-3bc7-4b5d-aedd-70a0915089b0",
				"applicationIds": ["77b6223b-eb41-4ccd-833c-c7b16537fde3"]
			}
			""";

		return Content(response, "application/json");
	}
}
