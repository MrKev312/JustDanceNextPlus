using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.users.me;

[ApiController]
[Route("v3/users/me/roamingProfiles")]
public class RoamingProfiles : ControllerBase
{
    [HttpGet]
    public IActionResult GetRoamingProfiles([FromQuery] string? spaceIds)
    {
        return Ok(new { roamingProfiles = Array.Empty<object>() });
    }
}
