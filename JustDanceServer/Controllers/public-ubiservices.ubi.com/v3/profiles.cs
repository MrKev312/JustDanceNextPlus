using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v3;

[ApiController]
[Route("v2/profiles")]
[Route("v3/profiles")]
public sealed class Profiles(IPlayerIdentityService playerIdentityService) : ControllerBase
{
    [HttpPost]
    public IActionResult PostProfiles([FromBody] object body) => Ok();

    [HttpGet]
    public async Task<IActionResult> GetProfiles(
        [FromQuery] string? userIds = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? idOnPlatform = null,
        [FromQuery] string? platformType = null,
        [FromQuery] string? profileId = null,
        [FromQuery] string? nameOnPlatform = null)
    {
        List<LinkedProfileIdentity> profiles = [];
        Guid[] requestedProfileIds = ParseGuids(profileId);
        if (requestedProfileIds.Length > 0)
        {
            profiles.AddRange(await playerIdentityService.GetProfilesByIdsAsync(
                requestedProfileIds,
                platformType));
        }

        // Ubisoft's user lookup uses the same /profiles resource but expects every
        // linked first-party profile, whereas profileId selects Uplay profiles.
        foreach (Guid requestedUserId in ParseGuids(userId, userIds))
            profiles.AddRange(await playerIdentityService.GetLinkedProfilesAsync(requestedUserId));

        string[] platformIds = Split(idOnPlatform);
        if (platformIds.Length > 0)
        {
            profiles.AddRange(await playerIdentityService.GetProfilesByPlatformIdsAsync(
                platformIds,
                platformType));
        }

        string[] names = Split(nameOnPlatform);
        if (names.Length > 0)
        {
            profiles.AddRange(await playerIdentityService.GetProfilesByNamesAsync(
                names,
                platformType));
        }

        return Ok(new
        {
            profiles = profiles
                .DistinctBy(profile => (profile.ProfileId, profile.PlatformType, profile.IdOnPlatform))
                .ToArray()
        });
    }

    private static Guid[] ParseGuids(params string?[] values)
        => [.. values
            .SelectMany(Split)
            .Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()];

    private static string[] Split(string? values)
        => string.IsNullOrWhiteSpace(values)
            ? []
            : values.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
