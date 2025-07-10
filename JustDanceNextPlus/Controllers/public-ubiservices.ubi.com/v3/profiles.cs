using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v3;

[ApiController]
[Route("v3/profiles")]
public class Profiles(UserDataService userDataService) : ControllerBase
{
	[HttpPost(Name = "PostProfiles")]
	public IActionResult GetProfiles([FromBody] object body)
	{
		string response = "{}";

		return Content(response, "application/json");
	}

	[HttpGet(Name = "GetProfiles")]
	public async Task<IActionResult> GetProfiles([FromQuery] string userIds)
	{
		Guid guid = Guid.Parse(userIds);

		Profile? user = await userDataService.GetProfileByIdAsync(guid);

		if (user == null)
			return NotFound();

		string response = $$"""
			   {
				"profiles": [{
					"profileId": "{{guid}}",
					"userId": "{{guid}}",
					"platformType": "uplay",
					"idOnPlatform": "{{guid}}",
					"nameOnPlatform": "{{user.Dancercard.Name}}"
				}, {
					"profileId": "{{guid}}",
					"userId": "{{guid}}",
					"platformType": "switch",
					"idOnPlatform": "f5dd2ga1258abc15",
					"nameOnPlatform": "{{user.Dancercard.Name}}"
				}]
			}
			""";

		return Content(response, "application/json");
	}
}