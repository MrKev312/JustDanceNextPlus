using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room;

[ApiController]
[Route("wdf/v1/rooms/{room}/online-rank-widget")]
public sealed class OnlineRankWidget(IWdfService wdfService, TimeProvider timeProvider) : ControllerBase
{
	[HttpGet]
	[HttpPost]
	public IActionResult Get([FromRoute] string room)
	{
		if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
			return BadRequest();

		return Ok(new WdfOnlineRankWidgetInfo
		{
			CurrentSeasonEndTime = timeProvider.GetUtcNow().AddMonths(1).ToUnixTimeSeconds(),
			CurrentSeasonDancerCount = wdfService.GetActivePlayerCount(resolvedRoom)
		});
	}
}

public sealed record WdfOnlineRankWidgetInfo
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "OnlineRankWidgetInfo";
	public long CurrentSeasonEndTime { get; init; }
	public int CurrentSeasonDancerCount { get; init; }
	public int SeasonNumber { get; init; } = 1;
}
