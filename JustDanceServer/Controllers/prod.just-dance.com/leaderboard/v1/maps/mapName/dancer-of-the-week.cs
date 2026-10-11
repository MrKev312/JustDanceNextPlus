using JustDanceServer.Services;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.leaderboard.v1.maps.mapName;

[ApiController]
[Route("leaderboard/v1/maps/{mapName}/dancer-of-the-week")]
public class DancerOfTheWeek(ILeaderboardService leaderboardService, IPlayerActivityService activity) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDancerOfTheWeek([FromRoute] string mapName)
    {
        PlayerMapPlay? winner = await activity.GetWeeklyWinnerAsync(mapName);
        LeaderboardPlayerScore? dancer = null;
        if (winner is not null)
        {
            LeaderboardPage leaderboard = await leaderboardService.GetDetailedLeaderboardAsync(
                mapName, 1, profileIds: [winner.ProfileId]);
            dancer = leaderboard.Entries.Count == 0 ? null : leaderboard.Entries[0];
        }

        return Ok(dancer is null
            ? new DancerOfTheWeekResponse()
            : new DancerOfTheWeekResponse
            {
                Score = winner!.Score,
                ProfileId = dancer.ProfileId,
                Rank = 1,
                Name = dancer.Name,
                Avatar = dancer.AvatarId,
                Country = dancer.CountryId,
                PlatformId = dancer.PlatformId,
                Alias = dancer.AliasId,
                AliasGender = dancer.AliasGender,
                JdPoints = dancer.JdPoints,
                PortraitBorder = dancer.PortraitBorderId
            });
    }
}

public record DancerOfTheWeekResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "DancerOfTheWeek";
    public int Score { get; init; }
    public Guid ProfileId { get; init; }
    public string GameVersion { get; init; } = "jd2022";
    public int Rank { get; init; }
    public string Name { get; init; } = "Player";
    public uint Avatar { get; init; }
    public int Country { get; init; }
    public string PlatformId { get; init; } = string.Empty;
    public uint Alias { get; init; }
    public int AliasGender { get; init; }
    public int JdPoints { get; init; }
    public uint PortraitBorder { get; init; }
}
