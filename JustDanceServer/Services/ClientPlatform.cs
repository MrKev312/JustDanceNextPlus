namespace JustDanceServer.Services;

public static class ClientPlatform
{
    public const string NintendoSwitch = "NX";
    public const string PC = "PC";
    public const string PlayStation5 = "PS5";
    public const string XboxSeries = "XboxScarlett";

    private static readonly IReadOnlyList<ClientPlatformDefinition> Definitions =
    [
        new(NintendoSwitch, [NintendoSwitch, "switch"], ["SWITCH64"], MapVideoCodec.Vp9),
        new(PC, [PC, "win", "windows"], ["PC64_ansi_dynamic"], MapVideoCodec.Vp8),
        new(PlayStation5, [PlayStation5, "prospero"], [], MapVideoCodec.Vp9),
        new(XboxSeries, [XboxSeries, "xbox-series"], [], MapVideoCodec.Vp9)
    ];

    private static readonly IReadOnlyDictionary<string, string> PlatformCodes = Definitions
        .SelectMany(definition => definition.Aliases.Select(alias => (Alias: alias, definition.Code)))
        .ToDictionary(entry => entry.Alias, entry => entry.Code, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlySet<string> SupportedPlatforms { get; } =
        Definitions.Select(definition => definition.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static string FromRequest(HttpRequest request)
    {
        string? platform = FromUserAgent(request.Headers.UserAgent.ToString());
        return platform
            ?? throw new BadHttpRequestException("The request does not identify a supported JDPlus platform.");
    }

    public static string? FromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        string trimmedUserAgent = userAgent.Trim();
        return Definitions
            .FirstOrDefault(definition => definition.UserAgentSuffixes.Any(suffix =>
                trimmedUserAgent.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            ?.Code;
    }

    public static string Normalize(string? platform)
    {
        if (!string.IsNullOrWhiteSpace(platform)
            && PlatformCodes.TryGetValue(platform.Trim(), out string? normalizedPlatform))
        {
            return normalizedPlatform;
        }

        throw new NotSupportedException($"Unsupported JDPlus platform '{platform ?? "<null>"}'.");
    }

    public static MapVideoCodec GetVideoCodec(string platform)
    {
        string normalizedPlatform = Normalize(platform);
        return Definitions.First(definition => definition.Code == normalizedPlatform).VideoCodec;
    }

    private sealed record ClientPlatformDefinition(
        string Code,
        IReadOnlyList<string> Aliases,
        IReadOnlyList<string> UserAgentSuffixes,
        MapVideoCodec VideoCodec);
}
