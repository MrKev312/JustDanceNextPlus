using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1.profile.map;

[ApiController]
[Route("progression/v1/profile/map/played")]
public class Played(IUserDataService userDataService) : ControllerBase
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

        // Build highscore performance
        var highScorePerformance = new HighscorePerformance
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
        (bool success, bool isNewHighScore) = await userDataService.UpdateHighScoreAsync(highScorePerformance, map.Score, profile.Id, map.MapId);
        if (!success)
            return BadRequest("Failed to update high score.");

		MapStats? MapStats = await userDataService.GetHighScoreByProfileIdAndMapIdAsync(profile.Id, map.MapId);

        return Ok(new {
            EarnedXP = 0,
            IsHighScore = isNewHighScore,
            IsLevelIncreased = false,
            IsPrestigeIncreased = false,
			profile.CurrentXP,
			profile.CurrentLevel,
            PrestigeGrade = 0,
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
