using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.songdb.v3.content_authorization.map.mapid.formatVersion;

[ApiController]
[Route("songdb/v3/content-authorization/map/{mapId:guid}/formatVersion/v0")]
public class V0(MapService mapService) : ControllerBase
{
	[HttpGet]
	public IActionResult Get([FromRoute] Guid mapId)
	{
		return mapService.SongDB.ContentAuthorization.TryGetValue(mapId, out ContentAuthorization? contentAuthorization)
			? Ok(contentAuthorization)
			: NotFound();
	}
}
