namespace JustDanceServer.Services;

public enum JduPlatform
{
    Pc,
    WiiU,
    PlayStation4,
    PlayStation5,
    XboxOne,
    NintendoSwitch,
    Stadia,
    XboxSeries,
    PlayStation3,
    Xbox360
}

public static class JduPlatformExtensions
{
    public static string GetUbisoftPlatformType(this JduPlatform platform)
        => platform switch
        {
            JduPlatform.Pc => "uplay",
            JduPlatform.WiiU => "wiiu",
            JduPlatform.PlayStation4 or JduPlatform.PlayStation5 => "psn",
            JduPlatform.XboxOne or JduPlatform.XboxSeries => "xbl",
            JduPlatform.NintendoSwitch => "switch",
            JduPlatform.Stadia => "googlestream",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };
}

public static class JduAssetPlatform
{
    private static readonly IReadOnlyList<JduAssetPlatformDefinition> Definitions =
    [
        new(JduPlatform.Pc, "PC", ["pc", "win", "windows"]),
        new(JduPlatform.WiiU, "WIIU", ["wiiu", "cafe"]),
        new(JduPlatform.PlayStation4, "ORBIS", ["ps4", "orbis"]),
        new(JduPlatform.PlayStation5, "PROSPERO", ["ps5", "prospero"]),
        new(JduPlatform.XboxOne, "DURANGO", ["xone", "xboxone", "durango"]),
        new(JduPlatform.NintendoSwitch, "NX", ["nx", "switch"]),
        new(JduPlatform.Stadia, "GGP", ["ggp", "stadia"]),
        new(JduPlatform.XboxSeries, "SCARLETT", ["scarlett", "xboxseries", "xseries"])
    ];

    public static IReadOnlyList<string> SupportedPlatforms { get; } =
        [.. Definitions.Select(definition => definition.Code)];

    public static string? FromSkuId(string? skuId)
    {
        if (string.IsNullOrWhiteSpace(skuId))
            return null;

        HashSet<string> tokens = skuId
            .Split(['-', '_', '.', '/', '\\', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        return Definitions
            .FirstOrDefault(definition => definition.SkuTokens.Any(tokens.Contains))
            ?.Code;
    }

    public static JduPlatform? GetPlatformFromSkuId(string? skuId)
    {
        string? code = FromSkuId(skuId);
        return code is null ? null : GetDefinition(code)?.Platform;
    }

    public static bool TryParse(string? assetPlatform, out JduPlatform platform)
    {
        JduAssetPlatformDefinition? definition = GetDefinition(assetPlatform);
        platform = definition?.Platform ?? default;
        return definition is not null;
    }

    public static string GetCode(JduPlatform platform)
        => Definitions.FirstOrDefault(definition => definition.Platform == platform)?.Code
            ?? throw new ArgumentOutOfRangeException(
                nameof(platform),
                platform,
                "The JDU platform is not supported.");

    public static string? Normalize(string? assetPlatform)
        => GetDefinition(assetPlatform)?.Code;

    private static JduAssetPlatformDefinition? GetDefinition(string? assetPlatform)
        => Definitions.FirstOrDefault(definition =>
            string.Equals(definition.Code, assetPlatform, StringComparison.OrdinalIgnoreCase));

    private sealed record JduAssetPlatformDefinition(
        JduPlatform Platform,
        string Code,
        IReadOnlyList<string> SkuTokens);
}
