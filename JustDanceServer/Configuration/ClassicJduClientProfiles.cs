using JustDanceServer.Services;

using System.Diagnostics.CodeAnalysis;

namespace JustDanceServer.Configuration;

public sealed record ClassicJduClientProfile(
    int GameYear,
    Guid ApplicationId,
    Guid SpaceId,
    JduPlatform ClientPlatform,
    IReadOnlyList<string>? PlatformApplicationIds = null) : IUbisoftClientProfile
{
    public string Platform => ClientPlatform switch
    {
        JduPlatform.PlayStation4 => "PS4",
        JduPlatform.XboxOne => "XONE",
        JduPlatform.NintendoSwitch => "SWITCH",
        _ => AssetPlatform
    };

    public string PlatformType => ClientPlatform.GetUbisoftPlatformType();
    public string AssetPlatform => JduAssetPlatform.GetCode(ClientPlatform);
}

public sealed record ClassicJduGameClients(
    int GameYear,
    IReadOnlyList<JduPlatformClient> Platforms)
{
    public IEnumerable<ClassicJduClientProfile> Expand()
        => Platforms.Select(platform => new ClassicJduClientProfile(
            GameYear,
            platform.ApplicationId,
            platform.SpaceId,
            platform.Platform,
            platform.PlatformApplicationIds));
}

public static class ClassicJduClientProfiles
{
    public static IReadOnlyList<ClassicJduGameClients> Games { get; } =
    [
        Jdu2016ClientProfiles.Game,
        Jdu2017ClientProfiles.Game,
        Jdu2018ClientProfiles.Game
    ];

    public static IReadOnlyList<ClassicJduClientProfile> All { get; } =
        [.. Games.SelectMany(game => game.Expand())];

    private static IReadOnlySet<int> GameYears { get; } =
        Games.Select(game => game.GameYear).ToHashSet();

    private static IReadOnlyDictionary<Guid, ClassicJduClientProfile> ByApplicationId { get; } =
        All.ToDictionary(profile => profile.ApplicationId);

    private static IReadOnlyDictionary<Guid, ClassicJduClientProfile> BySpaceId { get; } =
        All.ToDictionary(profile => profile.SpaceId);

    private static IReadOnlyDictionary<(int GameYear, JduPlatform Platform), ClassicJduClientProfile> ByGameAndPlatform { get; } =
        All.ToDictionary(profile => (profile.GameYear, profile.ClientPlatform));

    public static ClassicJduClientProfile GetClient(int gameYear, JduPlatform platform)
        => ByGameAndPlatform[(gameYear, platform)];

    public static bool TryGetByApplicationId(
        Guid applicationId,
        [NotNullWhen(true)] out ClassicJduClientProfile? profile)
        => ByApplicationId.TryGetValue(applicationId, out profile);

    public static bool TryGetBySpaceId(
        Guid spaceId,
        [NotNullWhen(true)] out ClassicJduClientProfile? profile)
        => BySpaceId.TryGetValue(spaceId, out profile);

    public static bool IsClassicSku(string? skuId)
        => TryGetGameYear(skuId, out _);

    public static bool TryGetGameYear(string? skuId, out int gameYear)
    {
        gameYear = 0;
        if (skuId is not { Length: >= 7 }
            || !skuId.StartsWith("jd", StringComparison.OrdinalIgnoreCase)
            || skuId[6] != '-'
            || !int.TryParse(skuId.AsSpan(2, 4), out int parsedYear)
            || !GameYears.Contains(parsedYear))
        {
            return false;
        }

        gameYear = parsedYear;
        return true;
    }

    public static bool TryGetBySkuId(
        string? skuId,
        [NotNullWhen(true)] out ClassicJduClientProfile? profile)
    {
        profile = null;
        return TryGetGameYear(skuId, out int gameYear)
            && JduAssetPlatform.GetPlatformFromSkuId(skuId) is JduPlatform platform
            && ByGameAndPlatform.TryGetValue((gameYear, platform), out profile);
    }
}
