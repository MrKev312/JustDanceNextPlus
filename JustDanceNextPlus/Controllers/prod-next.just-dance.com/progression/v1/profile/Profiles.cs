using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1.profile;

[ApiController]
[Route("progression/v1/profile/profiles")]
public class Profiles(IUserDataService userDataServices) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfiles([FromQuery] ProfilesRequest request)
    {
        List<ProfileData> profiles = [];
        foreach (string pid in request.ProfileIds)
		{
			if (!Guid.TryParse(pid, out Guid guid))
				continue;

			Profile? profile = await userDataServices.GetProfileByIdAsync(guid);
			if (profile == null)
				continue;

			profiles.Add(new ProfileData
			{
				Id = profile.Id,
				Dancercard = profile.Dancercard
			});
		}

		return Ok(new
        {
            Profiles = profiles
        });
    }

    public class ProfilesRequest
    {
        [FromQuery(Name = "pids")]
        public List<string> ProfileIds { get; set; } = [];
    }

    public class ProfileData
    {
        public required Guid Id { get; set; }
        public required DancerCard Dancercard { get; set; }
    }
}
