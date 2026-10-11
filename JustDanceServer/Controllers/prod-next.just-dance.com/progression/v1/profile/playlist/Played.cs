using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile.playlist;

[ApiController]
[Route("progression/v1/profile/playlist/played")]
public class Played(IUserDataService userDataService, IMapService mapService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PushPlaylistPlayedAsync([FromBody] PlayedWrapper body,
    [FromServices] ISessionManager sessionManager,
    [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        // Validate request and session header
        string? sessionId = httpContextAccessor.HttpContext?.Request.Headers["ubi-sessionid"].FirstOrDefault();
        if (body?.Playlist == null || sessionId == null)
            return BadRequest("Invalid request");

        PlayedRequest playlist = body.Playlist;

        if (playlist.MapData == null || playlist.MapData.Count == 0)
            return BadRequest("Invalid request");

        if (playlist.TotalScore < 0 || playlist.TotalScore > (playlist.MapData.Count * 13333))
            return BadRequest("Invalid request");

        if (playlist.MapData.Any(m => m.Score is < 0 or > 13333))
            return BadRequest("Invalid request");

        if (!Guid.TryParse(sessionId, out Guid sessionGuid))
            return Unauthorized("Invalid session");

        // Resolve session and profile
        Session? session = sessionManager.GetSessionById(sessionGuid);
        if (session == null)
            return Unauthorized("Invalid session");

        Profile? profile = await userDataService.GetProfileByIdAsync(session.PlayerId);
        if (profile == null)
            return NotFound("Profile not found");

        Dictionary<string, int> scoresByMapName = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (MapData map in playlist.MapData)
        {
            if (!mapService.TryGetMapName(map.MapId, out string mapName))
                return NotFound($"Map '{map.MapId}' was not found.");
            scoresByMapName[mapName] = map.Score;
        }

        // Build PlaylistStats to update
        PlaylistStats playlistStats = new()
        {
            PlayCount = 1,
            HighScore = playlist.TotalScore,
            PlaylistId = playlist.PlaylistId,
            ProfileId = profile.Id,
            Platform = ClientPlatform.FromUserAgent(Request.Headers.UserAgent) ?? "Unknown",
            HighScorePerMap = scoresByMapName
        };

        // Update using the user data service
        (bool success, bool isNewHighScore) = await userDataService.UpdatePlaylistHighScoreAsync(playlistStats, playlist.TotalScore, profile.Id, playlist.PlaylistId);

        // Retrieve stored playlist high score
        PlaylistStats? playlistHighScore = await userDataService.GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(profile.Id, playlist.PlaylistId);

        if (!success || (playlistHighScore == null && playlist.TotalScore != 0))
            return StatusCode(500, "Failed to update playlist high score");

        // Zero-point submissions still need the ordinary success envelope, but are not plays.
        playlistHighScore ??= new PlaylistStats
        {
            PlaylistId = playlist.PlaylistId,
            ProfileId = profile.Id,
            Platform = playlistStats.Platform,
            HighScorePerMap = scoresByMapName
        };

        return Ok(new
        {
            isHighScore = isNewHighScore,
            playlistStats = new
            {
                highScore = playlistHighScore.HighScore,
                playCount = playlistHighScore.PlayCount,
                highScorePerMap = playlistHighScore.HighScorePerMap.Select(kv => new
                {
                    mapId = mapService.GetSongId(kv.Key),
                    highScore = kv.Value
                }).Where(value => value.mapId.HasValue).ToArray(),
                platform = playlistHighScore.Platform
            }
        });
    }

    public class PlayedWrapper
    {
        public PlayedRequest? Playlist { get; set; }
    }

    public class PlayedRequest
    {
        public bool IsHighlighted { get; set; }
        public bool IsRecommended { get; set; }
        public List<MapData> MapData { get; set; } = [];
        public Guid PlaylistId { get; set; }
        public int TotalScore { get; set; }
        public int TotalStars { get; set; }
        public bool IsCoopEnabled { get; set; }
        public bool IsGroupEnabled { get; set; }
    }

    public class MapData
    {
        public Guid MapId { get; set; }
        public int Score { get; set; }
        public int Stars { get; set; }
    }
}
