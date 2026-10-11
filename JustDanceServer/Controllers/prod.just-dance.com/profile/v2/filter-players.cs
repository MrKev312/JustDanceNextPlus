using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.profile.v2;

[ApiController]
[Route("profile/v2/filter-players")]
public class FilterPlayers(IClassicProfileService profileService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Filter([FromBody] Guid[]? profileIds)
    {
        IReadOnlyList<Guid> filtered = await profileService.FilterExistingProfileIdsAsync(profileIds ?? []);
        return Ok(filtered);
    }
}
