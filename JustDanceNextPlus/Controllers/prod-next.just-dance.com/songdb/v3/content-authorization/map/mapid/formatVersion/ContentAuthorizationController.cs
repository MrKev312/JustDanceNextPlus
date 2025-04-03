using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.songdb.v3.content_authorization.map.mapid.formatVersion;

[ApiController]
[Route("songdb/v3/content-authorization/map/{mapId:guid}/formatVersion/v0")]
public class ContentAuthorizationController(ILogger<ContentAuthorizationController> logger,
	MapService mapService)
	: ControllerBase
{
	[HttpGet]
	public IActionResult Get([FromRoute] Guid mapId)
	{
		string mapCodename = mapService.MapToGuid
			.FirstOrDefault(x => x.Value == mapId).Key;

		if (string.IsNullOrEmpty(mapCodename))
		{
			logger.LogWarning("Map ID {MapId} not found in mapToGuid.", mapId);
			return NotFound();
		}

		logger.LogInformation("Map {MapCodename} requested.", mapCodename);

		return mapService.SongDB.ContentAuthorization.TryGetValue(mapId, out ContentAuthorization? contentAuthorization)
			? Ok(contentAuthorization)
			: NotFound();
	}
}
