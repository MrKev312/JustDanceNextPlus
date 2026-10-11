using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.carousel.v2.pages;

[ApiController]
[Route("carousel/v2/pages")]
public sealed class ClassicJdu(IClassicJduMenuService menuService, IMenuService sharedMenus,
    ISessionManager sessions) : ControllerBase
{
    [HttpPost("{pageName}", Order = 100)]
    public async Task<IActionResult> GetPage(
        [FromRoute] string pageName,
        [FromHeader(Name = "X-SkuId")] string? skuId,
        [FromBody] JduCarouselRequest? request = null)
    {
        if (!ClassicJduClientProfiles.TryGetBySkuId(skuId, out ClassicJduClientProfile? client))
            return NotFound();

        JduSongCarouselContentResponse response = menuService.CreatePage(pageName, client, request);
        if (string.Equals(pageName, "party", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request?.SearchString) && request?.SearchTags.Length is not > 0)
        {
            PlayerRequestContext context = PlayerRequestContextResolver.FromRequest(Request, sessions) with
            {
                GameYear = client.GameYear,
                Platform = client.PlatformType
            };
            IReadOnlyList<MenuRow> rows = await sharedMenus.GetRowsAsync("jdu-classic-party", context);
            response = MenuProjection.AddRows(response, rows);
        }

        return Ok(response);
    }
}
