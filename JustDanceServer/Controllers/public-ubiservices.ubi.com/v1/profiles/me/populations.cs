using JustDanceServer.Configuration;
using JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me.populations;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me;

[ApiController]
[Route("v1/profiles/me/populations")]
public class Populations(IClassicJduDiscoveryService classicDiscoveryService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetPopulations([FromQuery] string? spaceIds)
    {
        List<object> populations = [];

        foreach (Guid spaceId in ParseSpaceIds(spaceIds))
        {
            if (spaceId == Jdu2022ClientProfiles.Game.TargetSpaceId)
            {
                populations.AddRange(Data.CreateJdu2022Populations());
                continue;
            }

            if (ModernJduClientProfiles.IsKnownSpaceId(spaceId))
            {
                populations.Add(Data.CreateJdu2019Population(spaceId));
                continue;
            }

            if (ClassicJduClientProfiles.TryGetBySpaceId(
                spaceId,
                out ClassicJduClientProfile? classicProfile))
            {
                populations.Add(classicDiscoveryService.CreatePopulation(classicProfile));
            }
        }

        return Ok(new { populations });
    }

    private static IEnumerable<Guid> ParseSpaceIds(string? spaceIds)
        => spaceIds?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out Guid parsed) ? parsed : (Guid?)null)
            .OfType<Guid>()
            .Distinct()
            ?? [];
}
