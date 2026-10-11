using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database.Profile;

using System.Globalization;

namespace JustDanceServer.Services;

public sealed record PlayerRequestContext(
    Guid? ProfileId,
    ProgressionFamily Family,
    int? GameYear,
    string Platform,
    string Locale)
{
    public bool SupportsFavoriteMutations => Family == ProgressionFamily.JDUnlimited && GameYear == 2017;
    public bool SupportsMapHistory => Family == ProgressionFamily.JDUnlimited && GameYear is >= 2019 and <= 2022;
}

public static class PlayerRequestContextResolver
{
    public static PlayerRequestContext FromRequest(HttpRequest request, ISessionManager sessions)
    {
        RequestSessionResolver.TryResolve(request, sessions, out Session? session);
        Guid applicationId = Guid.TryParse(request.Headers["Ubi-AppId"].FirstOrDefault(), out Guid parsedApplicationId)
            ? parsedApplicationId : Guid.Empty;
        IUbisoftClientProfile? client = UbisoftClientProfiles.All.FirstOrDefault(value =>
            value.ApplicationId == (session?.UbiAppId ?? applicationId));
        int? year = client switch
        {
            ClassicJduClientProfile classic => classic.GameYear,
            ModernJduClientProfile modern => modern.GameYear,
            _ => session is null && WdfRequestResolver.TryGetGameYear(request, out int requestedYear)
                ? requestedYear : null
        };
        return new PlayerRequestContext(
            session?.PlayerId,
            year.HasValue ? ProgressionFamily.JDUnlimited : ProgressionFamily.JDPlus,
            year,
            client?.PlatformType ?? RequestSessionResolver.GetPlatform(request),
            LocaleFallback.Normalize(request.Headers.AcceptLanguage.FirstOrDefault()));
    }
}

public static class LocaleFallback
{
    public static string Normalize(string? locale)
    {
        string value = locale?.Split(',', ';')[0].Trim().Replace('_', '-') ?? string.Empty;
        try
        {
            return string.IsNullOrWhiteSpace(value) ? "en-US" : CultureInfo.GetCultureInfo(value).Name;
        }
        catch (CultureNotFoundException)
        {
            return "en-US";
        }
    }

    public static IEnumerable<string> GetCandidates(string? locale)
    {
        string normalized = Normalize(locale);
        return new[] { normalized, normalized.Split('-')[0], "en-US", "en" }
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static string Resolve(IReadOnlyDictionary<string, string> translations, string? locale, string fallback)
    {
        foreach (string candidate in GetCandidates(locale))
        {
            foreach ((string language, string text) in translations)
            {
                if (string.Equals(language, candidate, StringComparison.OrdinalIgnoreCase))
                    return text;
            }
        }

        return fallback;
    }
}
