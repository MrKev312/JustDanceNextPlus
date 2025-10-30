using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1;

[ApiController]
[Route("progression/v1/profile")]
public class ProfileEndpoint(IUserDataService userDataService, ISessionManager sessionManager) : ControllerBase
{
    [HttpPatch]
    public async Task<IActionResult> UpdateProfileAsync(
        [FromBody] DancerCard updatedProfile,
        [FromHeader(Name = "ubi-sessionid")] string? sessionId)
    {
        // Standardized error response
        if (updatedProfile == null || string.IsNullOrEmpty(sessionId) || !Guid.TryParse(sessionId, out Guid sessionGuid))
            return BadRequest("Invalid input");
        // Get the session
        Session? session = sessionManager.GetSessionById(sessionGuid);
        if (session == null)
            return BadRequest("Invalid session");
        // Get the user profile
        Profile? profile = await userDataService.GetProfileByIdAsync(session.PlayerId);
        if (profile == null)
            return NotFound("Profile not found");
        // Update the profile using the input
        profile.Dancercard = updatedProfile;
        // Update the profile in the database
        bool updated = await userDataService.UpdateProfileAsync(profile);
        if (!updated)
            return BadRequest("Failed to update profile");

        return Ok(new
        {
			profile.Dancercard
        });
    }
}
