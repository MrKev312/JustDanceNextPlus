using JustDanceServer.Services;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.home.v1;

[ApiController]
[Route("home/v1/tiles")]
public class Tiles(IModernJduMenuService menuService, IMenuService sharedMenus, ISessionManager sessions) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(131072)]
    public async Task<IActionResult> GetTiles([FromBody] JduHomeRequest request)
    {
        PlayerRequestContext context = PlayerRequestContextResolver.FromRequest(Request, sessions);
        EditorialAudience audience = new(EditorialProduct.JDUnlimited, context.GameYear, context.Platform, context.Locale);
        return Ok(menuService.CreateHomeData(audience, request,
            await sharedMenus.GetRowsAsync("jdu-home", context)));
    }
}
