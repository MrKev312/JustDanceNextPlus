using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.platform_support.v1;

[ApiController]
[Route("platform-support/v1/user-details")]
public sealed class UserDetails(
    IPlayerMetadataService metadataService,
    ISessionManager sessionManager) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetUserDetails()
    {
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();
        return Ok(new PlatformUserDetails
        {
            Age = 18,
            Country = (await metadataService.GetAsync(session.PlayerId)).CountryCode,
            Email = "player@justdance.local"
        });
    }
}

public sealed record PlatformUserDetails
{
    public int Age { get; init; }
    public required string Country { get; init; }
    public required string Email { get; init; }
}
