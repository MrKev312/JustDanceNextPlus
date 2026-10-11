using Microsoft.AspNetCore.Mvc;

using JustDanceServer.Services;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room;

[ApiController]
[Route("wdf/v1/rooms/{room}/newsfeed")]
public sealed class Newsfeed(IWdfService wdfService, IEditorialService editorialService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromRoute] string room)
        => WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom)
            ? Ok(new WdfNewsfeedList
            {
                Entries = [.. editorialService.GetEntries(new EditorialAudience(
                    EditorialProduct.JDUnlimited,
                    resolvedRoom.IsHappyHour ? null : resolvedRoom.GameYear,
                    RequestSessionResolver.GetPlatform(Request)))
                    .Select(entry => new WdfNewsfeedEntry(entry.Id, entry.Title, entry.Body))]
            })
            : BadRequest();
}

public sealed record WdfNewsfeedList
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "NewsfeedList";
    public IReadOnlyList<WdfNewsfeedEntry> Entries { get; init; } = [];
}

public sealed record WdfNewsfeedEntry(Guid Id, string Title, string Text);
