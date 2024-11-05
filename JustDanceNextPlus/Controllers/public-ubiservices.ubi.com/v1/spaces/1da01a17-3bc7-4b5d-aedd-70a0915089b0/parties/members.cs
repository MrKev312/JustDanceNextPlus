using Microsoft.AspNetCore.Mvc;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.parties;

[ApiController]
[Route("v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/parties/{partyID:guid}/members")]
public class Members(PartyManager partyManager) : ControllerBase
{
	[HttpGet(Name = "GetMembers")]
	public IActionResult GetMembers([FromRoute] Guid partyID)
	{
		Party? party = partyManager.GetParty(partyID);

		if (party == null)
			return NotFound();

		string response = $$"""
			{
				"ownerProfileId": "{{party.PartyMembers[0]}}",
				"members": [{
					"profileId": "{{party.PartyMembers[0]}}",
					"obj": {},
					"addedAt": "2024-10-07T09:32:22.063Z",
					"lastModifiedAt": "2024-10-07T09:32:22.063Z",
					"revision": 1
				}]
			}
			""";

		return Ok(response);
	}

	[HttpDelete("{profileId}", Name = "DeleteMember")]
	public IActionResult DeleteMember([FromRoute] Guid partyID, [FromRoute] Guid profileId)
	{
		partyManager.RemovePartyMember(partyID, profileId);

		return Ok();
	}
}
