using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1.profile.leaderboard.map;

[ApiController]
[Route("progression/v1/profile/leaderboard/map/profiles")]
public class Profiles(IUserDataService userDataService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetProfiles([FromBody] ProfilesRequest request)
    {
        Guid mapId = request.MapId;
		List<object> profiles = [];
        foreach (Guid userId in request.UsUserIds)
        {
			Profile? profile = await userDataService.GetProfileByIdAsync(userId);
            if (profile != null)
            {
                MapStats? mapStats = profile.MapStats.TryGetValue(mapId, out MapStats? stats) ? stats : null;
				ResponseProfile profileData = new()
				{
                    Id = profile.Id,
                    Dancercard = profile.Dancercard,
                    MapStat = mapStats,
                    Ownership = profile.Ownership
                };
                profiles.Add(profileData);
            }
        }

        return Ok(new
        {
            Profiles = profiles
        });
    }
}

public class ProfilesRequest
{
    public List<Guid> UsUserIds { get; set; } = [];
    public Guid MapId { get; set; }
}

public class ResponseProfile
{
    public Guid Id { get; set; }
    public DancerCard? Dancercard { get; set; }
    public MapStats? MapStat { get; set; }
    public Ownership? Ownership { get; set; }
}
