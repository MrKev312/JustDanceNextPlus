using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.applications;

[ApiController]
[Route("v2/applications")]
public sealed class Configuration(IModernJduApplicationConfigurationService configurationService) : ControllerBase
{
    [HttpGet("{applicationId:guid}/configuration")]
    public IActionResult GetConfiguration(Guid applicationId)
    {
        return ModernJduClientProfiles.TryGetByApplicationId(
            applicationId,
            out ModernJduClientProfile? profile)
                ? Ok(configurationService.Create(profile, $"https://{Request.Host.Value}"))
                : NotFound();
    }
}
