using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.challenge_match.v1;

[ApiController]
[Route("challenge-match/v1")]
public sealed class ChallengeMatches(
    IChallengeMatchService challengeMatches,
    ISessionManager sessionManager) : ControllerBase
{
    [HttpGet("challenge-by-map/{mapName}")]
    public async Task<IActionResult> GetChallenge([FromRoute] string mapName)
    {
        if (!TryGetPlayer(out Guid profileId, out IActionResult? error))
            return error;
        Guid? ugcId = await challengeMatches.FindChallengeForMapAsync(profileId, mapName);
        return ugcId.HasValue ? Ok(new { ugcId = ugcId.Value }) : NotFound();
    }

    [HttpPut("matches/{type}")]
    public async Task<IActionResult> Create(
        [FromRoute] string type,
        [FromBody] CreateChallengeMatchRequest request)
    {
        if (!TryGetPlayer(out Guid profileId, out IActionResult? error))
            return error;
        ChallengeMatchType? matchType = type.ToLowerInvariant() switch
        {
            "ranked" => ChallengeMatchType.Ranked,
            "friendly" => ChallengeMatchType.Friendly,
            _ => null
        };
        if (!matchType.HasValue)
            return NotFound();

        Guid? opponent = Guid.TryParse(request.OpponentPid, out Guid opponentId) ? opponentId : null;
        ChallengeMatchInfo? match = await challengeMatches.CreateAsync(
            profileId,
            matchType.Value,
            opponent,
            request.UgcId);
        return match is null
            ? NotFound("No score from a different player is available for this challenge.")
            : Ok(match);
    }

    [HttpPost("matches/{matchId:guid}/start-round")]
    public IActionResult StartRound([FromRoute] Guid matchId)
    {
        if (!TryGetPlayer(out Guid profileId, out IActionResult? error))
            return error;
        ChallengeMatchInfo? match = challengeMatches.StartRound(matchId, profileId);
        return match is null ? NotFound() : Ok(match);
    }

    [HttpPost("matches/{matchId:guid}/finalize-round")]
    public async Task<IActionResult> FinalizeRound(
        [FromRoute] Guid matchId,
        [FromBody] FinalizeChallengeRoundRequest request)
    {
        if (!TryGetPlayer(out Guid profileId, out IActionResult? error))
            return error;
        ChallengeMatchInfo? match = await challengeMatches.FinalizeRoundAsync(
            matchId,
            profileId,
            request.Score,
            RequestSessionResolver.GetPlatform(Request));
        return match is null ? NotFound() : Ok(match);
    }

    [HttpDelete("matches/{matchId:guid}")]
    public IActionResult Delete([FromRoute] Guid matchId)
    {
        if (!TryGetPlayer(out Guid profileId, out IActionResult? error))
            return error;
        return challengeMatches.Delete(matchId, profileId) ? Ok() : NotFound();
    }

    private bool TryGetPlayer(out Guid profileId, out IActionResult error)
    {
        if (RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
        {
            profileId = session.PlayerId;
            error = null!;
            return true;
        }

        profileId = Guid.Empty;
        error = Unauthorized();
        return false;
    }
}

public sealed record CreateChallengeMatchRequest(string OpponentPid, Guid? UgcId);
public sealed record FinalizeChallengeRoundRequest(int Score, int TauntId);
