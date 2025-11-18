using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.songdb.v3.content_authorization.map.mapid.formatVersion;

[ApiController]
[Route("songdb/v3/content-authorization/map/{mapId:guid}/formatVersion/v0")]
public class ContentAuthorizationController(ILogger<ContentAuthorizationController> logger,
	IMapService mapService,
	ISessionManager sessionManager,
	IUserDataService userDataService,
	DashboardService dashboardService)
	: ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> Get([FromRoute] Guid mapId)
	{
		// Get the session ID from the request headers
		if (!Request.Headers.TryGetValue("ubi-sessionid", out Microsoft.Extensions.Primitives.StringValues sessionIdValues)
			|| Guid.TryParse(sessionIdValues.ToString(), out Guid sessionId) == false)
		{
			logger.LogWarning("Missing 'ubi-sessionid' header in request.");
			return BadRequest("Missing 'ubi-sessionid' header in request.");
		}

		// Get the session from the session manager
		Session? session = sessionManager.GetSessionById(sessionId);

		if (session == null)
		{
			logger.LogWarning("Session not found for ID {SessionId}.", sessionId);
			return NotFound("Session not found.");
		}

		// Get the username from the session
		Guid playerId = session.PlayerId;
		Profile? user = await userDataService.GetProfileByIdAsync(playerId);

		if (user == null)
		{
			logger.LogWarning("Profile not found for player ID {PlayerId}.", playerId);
			return NotFound("Profile not found.");
		}

		string username = user.Dancercard.Name;

		string mapCodename = mapService.MapToGuid
			.FirstOrDefault(x => x.Value == mapId).Key;

		if (string.IsNullOrEmpty(mapCodename))
		{
			logger.LogWarning("Map ID {MapId} not found in mapToGuid.", mapId);
			return NotFound();
		}

		JustDanceSongDBEntry song = mapService.Songs[mapId];
		string songTitle = song.Title;
		if (song.DanceVersionLocId != 0)
			songTitle += $" ({song.DanceVersionLocId.Name})";

        logger.LogInformation("Map {MapCodename} requested by {Username}", mapCodename, username);
		dashboardService.LogRequest(mapCodename, songTitle, username);

		return mapService.ContentAuthorization.TryGetValue(mapId, out ContentAuthorization? contentAuthorization)
			? Ok(contentAuthorization)
			: NotFound();
	}
}
