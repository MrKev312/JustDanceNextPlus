using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile.map;

[ApiController]
[Route("progression/v1/profile/map/played")]
public class Played(
	IUserDataService userDataService,
	IMapService mapService,
	IMapCompletionService completions) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PushMapsPlayedAsync([FromBody] PlayedWrapper body,
    [FromServices] ISessionManager sessionManager,
    [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        // Validate input and session header
        string? sessionId = httpContextAccessor.HttpContext?.Request.Headers["ubi-sessionid"].FirstOrDefault();
        if (body?.Map == null || sessionId == null || body.Map.Score is < 0 or > 13333 || !Guid.TryParse(sessionId, out Guid sessionGuid))
            return BadRequest("Improperly formatted request.");

		// Resolve session and profile
		Session? session = sessionManager.GetSessionById(sessionGuid);
        if (session == null)
            return Unauthorized("Invalid or expired session.");

		Profile? profile = await userDataService.GetProfileByIdAsync(session.PlayerId);
        if (profile == null)
            return Unauthorized("Profile not found for the current session.");

		PlayedRequest map = body.Map;
		if (!mapService.TryGetMapName(map.MapId, out string mapName))
			return NotFound("Map not found.");

		// Build highscore performance
		HighscorePerformance highScorePerformance = new()
		{
            GoldMovesAchieved = map.GoldMovesAchieved,
            Moves = new MoveCounts
            {
                Missed = map.MissedMovesCount,
                Okay = map.OkayMovesCount,
                Good = map.GoodMovesCount,
                Super = map.SuperMovesCount,
                Perfect = map.PerfectMovesCount,
                Gold = map.GoldMovesCount
            }
        };

        // Update high score using the existing service
		string platform = ClientPlatform.FromUserAgent(Request.Headers.UserAgent) ?? "Unknown";
		MapCompletionResult completion = await completions.RecordAsync(new MapCompletion(
			profile.Id, mapName, map.Score, platform, map.IsCoopEnabled, highScorePerformance, GrantXp: true));
		(bool success, bool isNewHighScore) = completion.Score;
        if (!success)
            return BadRequest("Failed to update high score.");

		MapStats? MapStats = await userDataService.GetHighScoreByProfileIdAndMapNameAsync(profile.Id, mapName);
		MapStats ??= new MapStats
		{
			ProfileId = profile.Id,
			MapName = mapName,
			Platform = platform,
			HighScorePerformance = highScorePerformance
		};
		PlayerProgression progression = completion.Progression ?? new PlayerProgression
			{
				ProfileId = profile.Id,
				Family = ProgressionFamily.JDPlus,
				Xp = profile.CurrentXP,
				Level = profile.CurrentLevel,
				Prestige = profile.PrestigeGrade
			};

        return Ok(new {
			EarnedXP = Math.Clamp(map.Score, 0, 13_333) / 100,
            IsHighScore = isNewHighScore,
            IsLevelIncreased = false,
            IsPrestigeIncreased = false,
			CurrentXP = progression.Xp,
			CurrentLevel = progression.Level,
            PrestigeGrade = progression.Prestige,
			MapStats,
            EarnedSeasonPoints = 0
        });
    }

    public class PlayedWrapper
    {
        public PlayedRequest? Map { get; set; }
    }

    public class PlayedRequest
    {
        public Guid MapId { get; set; }
        public int Score { get; set; }
        public string GameMode { get; set; } = string.Empty;
        public double CompletionPercentage { get; set; }
        public int MissedMovesCount { get; set; }
        public int OkayMovesCount { get; set; }
        public int GoodMovesCount { get; set; }
        public int SuperMovesCount { get; set; }
        public int PerfectMovesCount { get; set; }
        public int GoldMovesCount { get; set; }
        public List<bool> GoldMovesAchieved { get; set; } = [];
        public int Stars { get; set; }
        public bool IsGroupEnabled { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsCoopEnabled { get; set; }
        public string LaunchTab { get; set; } = string.Empty;
        public int RemainingPlayersAtTheEnd { get; set; }
        public object? BossPlayedInfo { get; set; }
    }
}
