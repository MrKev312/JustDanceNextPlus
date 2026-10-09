using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.profile.v2.scores.maps;

[ApiController]
[Route("profile/v2/scores/maps/{mapName}")]
public sealed class MapName(IScoreService scoreService, ISessionManager sessionManager) : ControllerBase
{
	[HttpPut]
	public async Task<IActionResult> Put([FromRoute] string mapName, [FromBody] ScoreRequest request)
	{
		if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			return Unauthorized();
		if (string.IsNullOrWhiteSpace(mapName) || request.Score is < 0 or > 13333)
			return BadRequest();

		ScoreUpdateResult result = await scoreService.MergeMapScoreAsync(
			new HighscorePerformance(),
			request.Score,
			0,
			session.PlayerId,
			mapName,
			RequestSessionResolver.GetPlatform(Request));

		return result.Success ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
	}

	public sealed record ScoreRequest(int Score);
}
