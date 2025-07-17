using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v3;

[ApiController]
[Route("v3/profiles")]
public class Profiles(UserDataService userDataService) : ControllerBase
{
	[HttpPost(Name = "PostProfiles")]
	public IActionResult PostProfiles([FromBody] object body)
	{
		string response = "{}";
		return Content(response, "application/json");
	}

	[HttpGet(Name = "GetProfiles")]
	public async Task<IActionResult> GetProfiles([FromQuery] string userIds)
	{
		if (userIds == null)
			return BadRequest("No user IDs provided.");

		List<Guid> userIdList = [.. userIds.Split(',').Select(Guid.Parse)];
		if (userIdList.Count == 0)
			return BadRequest("No user IDs provided.");

		List<PlatformProfile> profiles = [];

		foreach (Guid userId in userIdList)
		{
			Profile? profile = await userDataService.GetProfileByIdAsync(userId);

			if (profile != null)
			{
				profiles.Add(new PlatformProfile(profile, "switch"));
				profiles.Add(new PlatformProfile(profile, "uplay"));
			}
		}

		return Ok(new
		{
			profiles
		});
	}

	public class PlatformProfile(Profile profile, string platformType = "switch")
	{
		public Guid ProfileId { get; set; } = profile.Id;
		public Guid UserId { get; set; } = profile.Id;
		public string PlatformType { get; set; } = platformType;
		public string IdOnPlatform { get; set; } = profile.Id.ToString();
		public string NameOnPlatform { get; set; } = profile.Dancercard.Name;
	}
}