using Microsoft.AspNetCore.Mvc;

using JustDanceServer.Services;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room;

[ApiController]
[Route("wdf/v1/rooms/{room}/notification")]
public sealed class Notification(
	TimeProvider timeProvider,
	IWdfService wdfService,
	IEditorialService editorialService) : ControllerBase
{
	[HttpGet]
	[HttpPost]
	public IActionResult Get([FromRoute] string room)
	{
		if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
			return BadRequest();
		IReadOnlyList<EditorialEntry> entries = editorialService.GetEntries(new EditorialAudience(
			EditorialProduct.JDUnlimited,
			resolvedRoom.IsHappyHour ? null : resolvedRoom.GameYear,
			RequestSessionResolver.GetPlatform(Request)));
		EditorialEntry? entry = entries.Count == 0 ? null : entries[0];

		return Ok(new WdfNotification
		{
			Title = entry?.Title ?? string.Empty,
			Name = entry?.Id.ToString() ?? string.Empty,
			NextNotificationCallTime = timeProvider.GetUtcNow().AddMinutes(5).ToUnixTimeMilliseconds() / 1000d
		});
	}
}

public sealed record WdfNotification
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "Notification";
	public string Title { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public double NextNotificationCallTime { get; init; }
	public double Duration { get; init; }
}
