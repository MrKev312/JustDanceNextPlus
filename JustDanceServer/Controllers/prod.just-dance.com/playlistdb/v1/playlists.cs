using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.playlistdb.v1;

[ApiController]
[Route("playlistdb/v1/playlists")]
public class Playlists(IModernJduMenuService menuService, IMenuService sharedMenus,
	ISessionManager sessions, IPlaylistService playlists, ILocalizedStringService text) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPlaylists([FromQuery] int? length)
    {
		PlayerRequestContext context = PlayerRequestContextResolver.FromRequest(Request, sessions);
		JduPlaylistDbResponse database = menuService.CreatePlaylistDatabase(length);
		foreach ((Guid id, ServerPlaylist playlist) in playlists.Playlists)
		{
			if (database.Db.TryGetValue(id.ToString("D"), out JduPlaylist? entry))
				database.Db[id.ToString("D")] = entry with
				{
					Title = playlist.TitleTag is null ? playlist.Title : text.ResolveTag(playlist.TitleTag, context.Locale),
					Description = playlist.DescriptionTag is null ? playlist.Description : text.ResolveTag(playlist.DescriptionTag, context.Locale)
				};
		}

		return Ok(MenuProjection.AddPlaylists(database,
			await sharedMenus.GetRowsAsync("jdu-playlists", context), length));
    }
}
