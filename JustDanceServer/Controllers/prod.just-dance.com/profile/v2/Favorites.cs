using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.profile.v2;

[ApiController]
[Route("profile/v2/favorites/maps/{mapName}")]
public sealed class Favorites(IPlayerPreferenceService preferences, ISessionManager sessions) : ControllerBase
{
	[HttpPut]
	public Task<IActionResult> Add([FromRoute] string mapName) => SetAsync(mapName, true);

	[HttpDelete]
	public Task<IActionResult> Remove([FromRoute] string mapName) => SetAsync(mapName, false);

	private async Task<IActionResult> SetAsync(string mapName, bool favorite)
	{
		PlayerRequestContext context = PlayerRequestContextResolver.FromRequest(Request, sessions);
		if (context.ProfileId is not Guid profileId)
			return Unauthorized();
		if (!context.SupportsFavoriteMutations)
			return NotFound();
		return await preferences.SetFavoriteAsync(profileId, mapName, favorite) ? Ok() : NotFound();
	}
}
