using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles;

[ApiController]
[Route("v1/profiles/me/applications")]
public sealed class Applications : ControllerBase
{
    [HttpGet]
    public IActionResult GetApplications()
        => Ok(new { applications = Array.Empty<object>() });
}

[ApiController]
[Route("v1/profiles/{profileId:guid}/actions")]
public sealed class Actions : ControllerBase
{
    [HttpGet]
    public IActionResult GetActions(Guid profileId)
    {
        _ = profileId;
        return Ok(new { actions = Array.Empty<object>() });
    }
}

[ApiController]
[Route("v1/profiles/{profileId:guid}/rewards")]
public sealed class Rewards : ControllerBase
{
    [HttpGet]
    public IActionResult GetRewards(Guid profileId)
    {
        _ = profileId;
        return Ok(new { rewards = Array.Empty<object>() });
    }
}
