using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1.profile.leaderboard.playlist;

[ApiController]
[Route("progression/v1/profile/leaderboard/playlist/profiles")]
public class Profiles(IUserDataService userDataService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetProfiles([FromBody] ProfilesRequest request)
    {
        Guid playlistId = request.PlaylistId;
		List<ResponseProfile> Profiles = [];
        foreach (Guid userId in request.UsUserIds)
        {
            Profile? profile = await userDataService.GetProfileByIdAsync(userId);
            if (profile != null)
            {
                PlaylistStats? playlistStats = profile.PlaylistStats.TryGetValue(playlistId, out PlaylistStats? stats) ? stats : null;
                ResponseProfile profileData = new()
                {
                    Id = profile.Id,
                    Dancercard = profile.Dancercard,
                    PlaylistStat = playlistStats,
                    Ownership = profile.Ownership
                };
                Profiles.Add(profileData);
            }
        }

        return Ok(new
        {
			Profiles
		});
    }
}

public class ProfilesRequest
{
    public List<Guid> UsUserIds { get; set; } = [];
    public Guid PlaylistId { get; set; }
}

public class ResponseProfile
{
    public Guid Id { get; set; }
    public DancerCard? Dancercard { get; set; }
    public PlaylistStats? PlaylistStat { get; set; }
    public Ownership? Ownership { get; set; }
}
