using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1.profile;

[ApiController]
[Route("progression/v1/profile/{guid:guid}")]
public class DancerCardGet(IUserDataService userDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDancerCard([FromRoute] Guid guid)
    {
		Profile? profile = await userDataService.GetProfileByIdAsync(guid);
        if (profile == null)
            return NotFound("Profile not found.");

        return Ok(new
        {
			profile.Dancercard
        });
    }
}
