using System.Security.Cryptography;
using System.Text;

using JustDanceServer.Configuration;

using Microsoft.Extensions.Options;

namespace JustDanceServer.Services;

public sealed record PlatformTicketContext(
    string PlatformType,
    string? IdOnPlatform,
    string NameOnPlatform,
    IReadOnlyList<string>? PlatformApplicationIds = null);

public sealed record VerifiedPlatformTicket(
    string Provider,
    string Subject,
    string IdOnPlatform);

public interface IPlatformTicketVerifier
{
    IReadOnlySet<string> AuthorizationSchemes { get; }

    Task<VerifiedPlatformTicket?> VerifyAsync(
        string authorizationScheme,
        string ticket,
        PlatformTicketContext context,
        CancellationToken cancellationToken = default);
}

public interface IPlatformTicketVerifierRegistry
{
    Task<VerifiedPlatformTicket?> VerifyAsync(
        string authorizationScheme,
        string ticket,
        PlatformTicketContext context,
        CancellationToken cancellationToken = default);
}

public sealed class PlatformTicketVerifierRegistry : IPlatformTicketVerifierRegistry
{
    private readonly IReadOnlyDictionary<string, IPlatformTicketVerifier> verifiers;

    public PlatformTicketVerifierRegistry(IEnumerable<IPlatformTicketVerifier> verifiers)
    {
        Dictionary<string, IPlatformTicketVerifier> byScheme =
            [with(StringComparer.OrdinalIgnoreCase)];
        foreach (IPlatformTicketVerifier verifier in verifiers)
        {
            foreach (string scheme in verifier.AuthorizationSchemes)
            {
                if (!byScheme.TryAdd(scheme, verifier))
                    throw new InvalidOperationException(
                        $"Multiple platform-ticket verifiers handle authorization scheme '{scheme}'.");
            }
        }

        this.verifiers = byScheme;
    }

    public Task<VerifiedPlatformTicket?> VerifyAsync(
        string authorizationScheme,
        string ticket,
        PlatformTicketContext context,
        CancellationToken cancellationToken = default)
        => verifiers.TryGetValue(authorizationScheme, out IPlatformTicketVerifier? verifier)
            ? verifier.VerifyAsync(authorizationScheme, ticket, context, cancellationToken)
            : Task.FromResult<VerifiedPlatformTicket?>(null);
}

public sealed class DevelopmentPlatformTicketVerifier(
    IOptions<PlatformAuthenticationSettings> settings) : IPlatformTicketVerifier
{
    private static readonly IReadOnlyDictionary<string, string> ProvidersByScheme =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["wiiu"] = "nintendo-network",
            ["uplaypc_v1"] = "ubisoft-ticket",
            ["ubi_v1"] = "ubisoft-session",
            ["ubimobile_v1"] = "ubisoft-mobile",
            ["ubimobile_v2"] = "ubisoft-mobile",
            ["psn2.0"] = "playstation-network",
            ["psn5.1"] = "playstation-network",
            ["XBL3.0"] = "xbox-network"
        };

    public IReadOnlySet<string> AuthorizationSchemes { get; } =
        new HashSet<string>(ProvidersByScheme.Keys, StringComparer.OrdinalIgnoreCase);

    public Task<VerifiedPlatformTicket?> VerifyAsync(
        string authorizationScheme,
        string ticket,
        PlatformTicketContext context,
        CancellationToken cancellationToken = default)
    {
        if (!settings.Value.AllowUnverifiedPlatformTickets
            || !ProvidersByScheme.TryGetValue(authorizationScheme, out string? schemeProvider))
        {
            return Task.FromResult<VerifiedPlatformTicket?>(null);
        }

        string subject = context.IdOnPlatform ?? Fingerprint(ticket);
        return Task.FromResult<VerifiedPlatformTicket?>(new(
            schemeProvider,
            subject,
            context.IdOnPlatform ?? subject));
    }

    private static string Fingerprint(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
