using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room.themes.theme;

[ApiController]
[Route("wdf/v1/rooms/{room}/themes/{theme}")]
public sealed class Scores(IWdfService wdfService, ISessionManager sessionManager) : ControllerBase
{
    [HttpGet("score-status")]
    public async Task<IActionResult> GetStatus(
        [FromRoute] string room,
        [FromRoute] string theme)
    {
        if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
            return BadRequest();
        if (!TryGetPlayer(out Session? session))
            return Unauthorized();

        WdfInitScoreResult result = await wdfService.InitializeScoreAsync(
            resolvedRoom,
            session.PlayerId,
            RequestSessionResolver.GetPlatform(Request));
        return Ok(result);
    }

    [HttpPost("update-scores")]
    public async Task<IActionResult> Update(
        [FromRoute] string room,
        [FromRoute] string theme,
        [FromBody] WdfScoreUpdateRequest request)
    {
        if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
            return BadRequest();
        if (!TryGetPlayer(out Session? session))
            return Unauthorized();
        if (!double.IsFinite(request.Score) || request.Score is < 0 or > 13_333)
            return BadRequest();

        WdfUpdateScoreResult result = await wdfService.UpdateScoreAsync(
            resolvedRoom,
            session.PlayerId,
            RequestSessionResolver.GetPlatform(Request),
            request.Score);
        return Ok(result);
    }

    [HttpGet("score-recap")]
    [HttpPost("score-recap")]
    public IActionResult GetRecap(
        [FromRoute] string room,
        [FromRoute] string theme)
    {
        if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
            return BadRequest();
        if (!TryGetPlayer(out Session? session))
            return Unauthorized();

        return Ok(wdfService.GetRecap(resolvedRoom, session.PlayerId));
    }

    private bool TryGetPlayer([NotNullWhen(true)] out Session? session)
        => RequestSessionResolver.TryResolve(Request, sessionManager, out session);
}

public sealed record WdfScoreUpdateRequest
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Score { get; init; }
    public string EncryptedScore { get; init; } = string.Empty;
}
