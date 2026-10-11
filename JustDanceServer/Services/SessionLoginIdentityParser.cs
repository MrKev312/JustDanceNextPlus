using System.Text;
using System.Text.Json;

namespace JustDanceServer.Services;

public sealed record SessionLoginIdentity(
    string Provider,
    string Subject,
    string PlatformType,
    string IdOnPlatform,
    string NameOnPlatform,
    string LegacyTicket)
{
    public string SessionKey => $"{Provider}:{Subject}";
}

public interface ISessionLoginIdentityParser
{
    Task<SessionLoginIdentity?> ParseAsync(
        string authorization,
        JsonElement body,
        string platformType,
        string nameOnPlatform,
        IReadOnlyList<string>? platformApplicationIds = null,
        CancellationToken cancellationToken = default);
}

public sealed class SessionLoginIdentityParser(
    IPlatformTicketVerifierRegistry ticketVerifiers,
    IBasicCredentialVerifier? basicCredentials = null)
    : ISessionLoginIdentityParser
{
    public async Task<SessionLoginIdentity?> ParseAsync(
        string authorization,
        JsonElement body,
        string platformType,
        string nameOnPlatform,
        IReadOnlyList<string>? platformApplicationIds = null,
        CancellationToken cancellationToken = default)
    {
        platformType = NormalizePlatform(platformType);
        string? idOnPlatform = GetOptionalScalar(body, "idOnPlatform");

        if (authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            if (platformType != "uplay" || basicCredentials is null)
                return null;
            string credentials = authorization["Basic ".Length..].Trim();
            if (credentials.Length == 0)
                return null;

            string login;
            string password;
            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(credentials));
                int separator = decoded.IndexOf(':');
                if (separator <= 0)
                    return null;
                login = decoded[..separator];
                password = decoded[(separator + 1)..];
            }
            catch (FormatException)
            {
                return null;
            }

            string legacyLogin = login.Trim();
            login = legacyLogin.ToLowerInvariant();
            if (login.Length == 0 || !basicCredentials.Verify(login, password))
                return null;

            return new SessionLoginIdentity(
                "ubisoft-login",
                login,
                platformType,
                idOnPlatform ?? login,
                nameOnPlatform,
                legacyLogin);
        }

        if (TryReadPlatformTicket(authorization, out string authorizationScheme, out string ticket))
        {
            VerifiedPlatformTicket? verified = await ticketVerifiers.VerifyAsync(
                authorizationScheme,
                ticket,
                new PlatformTicketContext(
                    platformType,
                    idOnPlatform,
                    nameOnPlatform,
                    platformApplicationIds),
                cancellationToken);
            return verified is null
                ? null
                : new SessionLoginIdentity(
                    verified.Provider,
                    verified.Subject,
                    platformType,
                    verified.IdOnPlatform,
                    nameOnPlatform,
                    ticket);
        }

        return null;
    }

    private static string? GetOptionalScalar(JsonElement body, string propertyName)
    {
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty(propertyName, out JsonElement property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => Normalize(property.GetString()),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizePlatform(string platformType)
        => string.IsNullOrWhiteSpace(platformType)
            ? "unknown"
            : platformType.Trim().ToLowerInvariant();

    private static bool TryReadPlatformTicket(
        string authorization,
        out string authorizationScheme,
        out string ticket)
    {
        int separator = authorization.IndexOf(" t=", StringComparison.OrdinalIgnoreCase);
        if (separator > 0)
        {
            authorizationScheme = authorization[..separator].Trim();
            ticket = authorization[(separator + 3)..].Trim();
            return authorizationScheme.Length > 0 && ticket.Length > 0;
        }

        int schemeSeparator = authorization.IndexOf(' ');
        if (schemeSeparator > 0
            && authorization[..schemeSeparator].Equals("XBL3.0", StringComparison.OrdinalIgnoreCase))
        {
            authorizationScheme = authorization[..schemeSeparator].Trim();
            ticket = authorization[(schemeSeparator + 1)..].Trim();
            return ticket.Length > 0;
        }

        authorizationScheme = string.Empty;
        ticket = string.Empty;
        return false;
    }
}
