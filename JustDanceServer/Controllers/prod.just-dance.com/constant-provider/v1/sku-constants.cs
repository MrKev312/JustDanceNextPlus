using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.constant_provider.v1;

[ApiController]
[Route("constant-provider/v1/sku-constants")]
public sealed class SkuConstants(IClassicJduConstantService constantService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetConstants()
        => Ok(constantService.CreateConstants());
}
