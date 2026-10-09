namespace JustDanceServer.Configuration;

public interface IUbisoftClientProfile
{
    Guid ApplicationId { get; }
    Guid SpaceId { get; }
    string PlatformType { get; }
    IReadOnlyList<string>? PlatformApplicationIds { get; }
    Guid? TargetSpaceId => null;
    bool UseDiscoveryHost => false;
}

public sealed record UbisoftClientProfile(
    Guid ApplicationId,
    Guid SpaceId,
    string PlatformType,
    IReadOnlyList<string>? PlatformApplicationIds = null,
    Guid? TargetSpaceId = null,
    bool UseDiscoveryHost = false) : IUbisoftClientProfile;

public static class UbisoftClientProfiles
{
    public static IReadOnlyList<IUbisoftClientProfile> All { get; } =
    [
        new UbisoftClientProfile(
            JdNextConstants.PcApplicationId,
            JdNextConstants.SessionSpaceId,
            "uplay",
            TargetSpaceId: JdNextConstants.GameSpaceId),
        new UbisoftClientProfile(
            JdNextConstants.SwitchApplicationId,
            JdNextConstants.SessionSpaceId,
            "uplay",
            [JdNextConstants.SwitchTitleId],
            JdNextConstants.GameSpaceId),
        .. JduApplicationClientProfiles.All
    ];
}

public static class JduApplicationClientProfiles
{
    public static IReadOnlySet<int> GameYears { get; } =
        ClassicJduClientProfiles.Games.Select(game => game.GameYear)
            .Concat(ModernJduClientProfiles.Games.Select(game => game.GameYear))
            .ToHashSet();

    public static IReadOnlyList<IUbisoftClientProfile> All { get; } =
    [
        .. ClassicJduClientProfiles.All,
        .. ModernJduClientProfiles.All
    ];
}
