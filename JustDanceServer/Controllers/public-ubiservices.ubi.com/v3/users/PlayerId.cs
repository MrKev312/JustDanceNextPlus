using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.users;

[ApiController]
[Route("v3/users/{guid:guid}")]
public class PlayerId(JustDanceServer.Services.IUserDataService userDataService) : ControllerBase
{
	// Get: /public-ubiservices.ubi.com/v3/users/{guid}
	// Simply log the request and return an empty json response
	[HttpGet(Name = "GetUserLanguages")]
	public IActionResult GetUserLanguages([FromRoute] Guid guid)
	{
		string response = """
			{
				"country": "NL",
				"preferredLanguage": "en"
			}
			""";

		return Content(response, "application/json");
	}

	[HttpGet("profiles")]
	public async Task<IActionResult> GetLinkedProfiles([FromRoute] Guid guid)
	{
		JustDanceServer.JustDanceClasses.Database.Profile.Profile? profile = await userDataService.GetProfileByIdAsync(guid);
		string nameOnPlatform = profile?.Dancercard.Name ?? "John Doe";

		return Ok(new
		{
			Profiles = new[]
			{
				new LinkedProfile(guid, nameOnPlatform, "uplay"),
				new LinkedProfile(guid, nameOnPlatform, "switch")
			}
		});
	}

	public class LinkedProfile(Guid userId, string nameOnPlatform, string platformType)
	{
		public string IdOnPlatform { get; set; } = userId.ToString();
		public string NameOnPlatform { get; set; } = nameOnPlatform;
		public string PlatformType { get; set; } = platformType;
		public Guid ProfileId { get; set; } = userId;
		public Guid UserId { get; set; } = userId;
	}
}
