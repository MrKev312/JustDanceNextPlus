using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.applications;

[ApiController]
[Route("v1/applications")]
public sealed class Configuration(IClassicJduConfigurationService configurationService) : ControllerBase
{
    [HttpGet("{applicationId:guid}/configuration")]
    public IActionResult GetConfiguration(Guid applicationId)
    {
        return ClassicJduClientProfiles.TryGetByApplicationId(
            applicationId,
            out ClassicJduClientProfile? profile)
                ? Ok(configurationService.Create(profile, $"https://{Request.Host.Value}"))
                : NotFound();
    }
}
