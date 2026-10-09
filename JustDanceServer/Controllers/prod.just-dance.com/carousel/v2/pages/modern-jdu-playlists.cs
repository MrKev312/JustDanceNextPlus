using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.carousel.v2.pages;

[ApiController]
[Route("carousel/v2/pages")]
public class ModernJduPlaylists(IModernJduMenuService menuService, IMenuService sharedMenus,
	ISessionManager sessions) : ControllerBase
{
    [HttpPost("jd{gameYear:int}-playlists")]
    public async Task<IActionResult> GetPlaylists(int gameYear)
    {
		if (gameYear is < 2019 or > 2022)
			return NotFound();
		IReadOnlyList<MenuRow> rows = await sharedMenus.GetRowsAsync("jdu-playlists",
			PlayerRequestContextResolver.FromRequest(Request, sessions));
		return Ok(MenuProjection.AddPlaylists(menuService.CreatePlaylistCarousel(), rows));
    }

    [HttpPost("anthology")]
    public IActionResult GetAnthology()
    {
        return Ok(menuService.CreateAnthologyCarousel());
    }
}
