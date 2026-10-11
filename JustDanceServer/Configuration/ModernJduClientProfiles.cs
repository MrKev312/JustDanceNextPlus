using JustDanceServer.Services;

using System.Diagnostics.CodeAnalysis;

namespace JustDanceServer.Configuration;

public sealed record ModernJduClientProfile(
    int GameYear,
    Guid ApplicationId,
    Guid SpaceId,
    JduPlatform ClientPlatform,
    IReadOnlyList<string>? PlatformApplicationIds = null,
    Guid? TargetSpaceId = null,
    bool UseDiscoveryHost = false) : IUbisoftClientProfile
{
    public string Platform => JduAssetPlatform.GetCode(ClientPlatform);
    public string PlatformType => ClientPlatform.GetUbisoftPlatformType();
}

public sealed record ModernJduGameClients(
    int GameYear,
    IReadOnlyList<JduPlatformClient> Platforms,
    Guid? TargetSpaceId = null,
    bool UseDiscoveryHost = false)
{
    public IEnumerable<ModernJduClientProfile> Expand()
        => Platforms.Select(platform => new ModernJduClientProfile(
            GameYear,
            platform.ApplicationId,
            platform.SpaceId,
            platform.Platform,
            platform.PlatformApplicationIds,
            TargetSpaceId,
            UseDiscoveryHost));
}

public static class ModernJduClientProfiles
{
    public static IReadOnlyList<ModernJduGameClients> Games { get; } =
    [
        Jdu2019ClientProfiles.Game,
        Jdu2020ClientProfiles.Game,
        Jdu2021ClientProfiles.Game,
        Jdu2022ClientProfiles.Game
    ];

    public static IReadOnlyList<ModernJduClientProfile> All { get; } =
        [.. Games.SelectMany(game => game.Expand())];

    private static IReadOnlyDictionary<Guid, ModernJduClientProfile> ByApplicationId { get; } =
        All.ToDictionary(profile => profile.ApplicationId);

    private static IReadOnlyDictionary<Guid, ModernJduClientProfile> BySpaceId { get; } =
        All.ToDictionary(profile => profile.SpaceId);

    private static IReadOnlySet<Guid> GameSpaceIds { get; } =
        Games.Where(game => game.TargetSpaceId.HasValue)
            .Select(game => game.TargetSpaceId!.Value)
            .ToHashSet();

    public static bool IsKnownSpaceId(Guid spaceId)
        => BySpaceId.ContainsKey(spaceId) || GameSpaceIds.Contains(spaceId);

    public static bool TryGetByApplicationId(
        Guid applicationId,
        [NotNullWhen(true)] out ModernJduClientProfile? profile)
        => ByApplicationId.TryGetValue(applicationId, out profile);

    public static bool TryGetBySpaceId(
        Guid spaceId,
        [NotNullWhen(true)] out ModernJduClientProfile? profile)
        => BySpaceId.TryGetValue(spaceId, out profile);
}
