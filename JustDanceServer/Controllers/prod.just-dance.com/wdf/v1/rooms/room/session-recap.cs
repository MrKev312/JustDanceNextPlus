using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room;

[ApiController]
[Route("wdf/v1/rooms/{room}/session-recap")]
public sealed class SessionRecap(IWdfService wdfService, ISessionManager sessionManager) : ControllerBase
{
    [HttpGet]
    [HttpPost]
    public IActionResult Get([FromRoute] string room)
    {
        if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
            return BadRequest();

        Guid playerId = RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session)
            ? session.PlayerId
            : Guid.Empty;
        WdfSessionSummary summary = wdfService.GetSessionSummary(playerId);
        WdfRecapInfo recap = wdfService.GetRecap(resolvedRoom, playerId);
        return Ok(new WdfSessionRecapInfo
        {
            UniquePlayerCount = summary.UniquePlayerCount,
            Countries = summary.Countries,
            Leaderboard = CreateLeaderboard(recap.RecapEntries)
        });
    }

    private static IReadOnlyList<WdfOnlineRankInfo> CreateLeaderboard(
        IReadOnlyList<WdfScoreEntry> entries)
    {
        List<WdfOnlineRankInfo> result = [];
        double? previousScore = null;
        int previousRank = 0;
        for (int index = 0; index < entries.Count; index++)
        {
            WdfScoreEntry entry = entries[index];
            int rank = previousScore == entry.Score ? previousRank : index + 1;
            result.Add(new WdfOnlineRankInfo
            {
                Rank = rank,
                WdfPoints = 0,
                DancerCard = entry
            });
            previousScore = entry.Score;
            previousRank = rank;
        }

        return result;
    }
}

public sealed record WdfSessionRecapInfo
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "SessionRecapInfo";
    public int UniquePlayerCount { get; init; }
    public IReadOnlyList<string> Countries { get; init; } = [];
    [JsonPropertyName("lb")]
    public IReadOnlyList<WdfOnlineRankInfo> Leaderboard { get; init; } = [];
}
