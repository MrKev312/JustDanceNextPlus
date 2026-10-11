using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.users;

[ApiController]
[Route("v3/users/{guid:guid}")]
public sealed class PlayerId(IPlayerIdentityService playerIdentityService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetUserLanguages([FromRoute] Guid guid)
        => Ok(new
        {
            country = "NL",
            preferredLanguage = "en"
        });

    [HttpGet("profiles")]
    public async Task<IActionResult> GetLinkedProfiles([FromRoute] Guid guid)
        => Ok(new
        {
            profiles = await playerIdentityService.GetLinkedProfilesAsync(guid)
        });
}
