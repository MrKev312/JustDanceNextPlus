using JustDanceServer.Configuration;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace JustDanceServer.Services;

public sealed class NintendoPlatformTicketVerifier(
	IHttpClientFactory httpClientFactory,
	IOptions<PlatformAuthenticationSettings> settings,
	ILogger<NintendoPlatformTicketVerifier> logger) : IPlatformTicketVerifier
{
	private static readonly TimeSpan KeySetLifetime = TimeSpan.FromHours(6);
	private readonly ConcurrentDictionary<Uri, CachedKeySet> keySets = new();
	private readonly SemaphoreSlim keySetLock = new(1, 1);

	public IReadOnlySet<string> AuthorizationSchemes { get; } =
		new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "switch" };

	public async Task<VerifiedPlatformTicket?> VerifyAsync(
		string authorizationScheme,
		string encodedToken,
		PlatformTicketContext context,
		CancellationToken cancellationToken = default)
	{
		if (settings.Value.AllowUnverifiedPlatformTickets)
		{
			logger.LogWarning(
				"Platform-ticket validation is bypassed by development configuration for {Scheme}",
				authorizationScheme);
			return CreateUnverifiedIdentity(encodedToken, context);
		}

		JwtSecurityToken token;
		try
		{
			token = new JwtSecurityTokenHandler().ReadJwtToken(encodedToken);
		}
		catch (ArgumentException)
		{
			return null;
		}

		if (!TryReadIdentityClaims(token, context, out string subject))
			return null;

		if (!token.Header.TryGetValue("jku", out object? jku)
			|| !string.Equals(token.Header.Alg, SecurityAlgorithms.RsaSha256, StringComparison.Ordinal)
			|| string.IsNullOrWhiteSpace(token.Header.Kid)
			|| !TryGetTrustedKeySetUri(jku?.ToString(), out Uri? keySetUri)
			|| !string.Equals(token.Issuer, keySetUri!.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal)
			|| !token.Payload.TryGetValue("typ", out object? tokenType)
			|| !string.Equals(tokenType?.ToString(), "id_token", StringComparison.Ordinal))
		{
			return null;
		}

		try
		{
			IReadOnlyCollection<SecurityKey> keys = await GetKeysAsync(keySetUri, false, cancellationToken);
			SecurityKey[] matchingKeys = [.. keys.Where(key => string.Equals(key.KeyId, token.Header.Kid, StringComparison.Ordinal))];
			if (matchingKeys.Length == 0)
			{
				keys = await GetKeysAsync(keySetUri, true, cancellationToken);
				matchingKeys = [.. keys.Where(key => string.Equals(key.KeyId, token.Header.Kid, StringComparison.Ordinal))];
			}

			if (matchingKeys.Length == 0)
				return null;

			TokenValidationParameters parameters = new()
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKeys = matchingKeys,
				RequireSignedTokens = true,
				ValidateIssuer = true,
				ValidIssuer = keySetUri.GetLeftPart(UriPartial.Authority),
				ValidateAudience = true,
				ValidAudiences = settings.Value.Nintendo.ValidAudiences,
				ValidateLifetime = true,
				RequireExpirationTime = true,
				ClockSkew = TimeSpan.FromMinutes(1),
				ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
			};

			JwtSecurityTokenHandler handler = new() { MapInboundClaims = false };
			ClaimsPrincipal principal = handler.ValidateToken(
				encodedToken,
				parameters,
				out _);
			return CreateIdentity(principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
		}
		catch (Exception exception) when (exception is
			SecurityTokenException or
			HttpRequestException or
			System.Text.Json.JsonException or
			ArgumentException)
		{
			logger.LogWarning(exception, "Nintendo JWT validation failed");
			return null;
		}
	}

	private static VerifiedPlatformTicket? CreateIdentity(string? subject)
	{
		subject = NormalizeSubject(subject);
		return subject is null
			? null
			: new VerifiedPlatformTicket(
				"nintendo-account",
				subject,
				subject);
	}

	private static VerifiedPlatformTicket CreateUnverifiedIdentity(
		string encodedToken,
		PlatformTicketContext context)
	{
		string? subject = context.IdOnPlatform;
		if (subject is null)
		{
			try
			{
				subject = NormalizeSubject(
					new JwtSecurityTokenHandler().ReadJwtToken(encodedToken).Subject);
			}
			catch (ArgumentException)
			{
				// Development bypass accepts malformed and unsigned tokens too.
			}
		}

		subject ??= Convert.ToHexString(
			SHA256.HashData(Encoding.UTF8.GetBytes(encodedToken))).ToLowerInvariant();
		return new VerifiedPlatformTicket("nintendo-account", subject, subject);
	}

	private bool TryReadIdentityClaims(
		JwtSecurityToken token,
		PlatformTicketContext context,
		out string subject)
	{
		subject = NormalizeSubject(token.Subject) ?? string.Empty;
		string[] validAudiences = settings.Value.Nintendo.ValidAudiences ?? [];
		if (subject.Length == 0
			|| !token.Payload.TryGetValue("typ", out object? tokenType)
			|| !string.Equals(tokenType?.ToString(), "id_token", StringComparison.Ordinal)
			|| validAudiences.Length == 0
			|| !token.Audiences.Any(audience => validAudiences.Contains(
				audience,
				StringComparer.Ordinal))
			|| context.PlatformApplicationIds is not { Count: > 0 }
			|| !TryGetNintendoApplicationId(token.Payload, out string? applicationId)
			|| !context.PlatformApplicationIds.Contains(
				applicationId,
				StringComparer.OrdinalIgnoreCase))
		{
			subject = string.Empty;
			return false;
		}

		return true;
	}

	private static bool TryGetNintendoApplicationId(
		JwtPayload payload,
		out string applicationId)
	{
		applicationId = string.Empty;
		string? value = payload.TryGetValue("nintendo.ai", out object? flatApplicationId)
			? flatApplicationId?.ToString()
			: null;
		if (value is null && payload.TryGetValue("nintendo", out object? nintendo))
		{
			try
			{
				JsonElement element = JsonSerializer.SerializeToElement(nintendo);
				if (element.ValueKind == JsonValueKind.Object
					&& element.TryGetProperty("ai", out JsonElement ai))
				{
					value = ai.GetString();
				}
			}
			catch (NotSupportedException)
			{
				return false;
			}
		}

		value = value?.Trim().ToLowerInvariant();
		if (value is null
			|| value.Length != 16
			|| !value.AsSpan().ContainsOnlyHexDigits())
			return false;

		applicationId = value;
		return true;
	}

	private async Task<IReadOnlyCollection<SecurityKey>> GetKeysAsync(
		Uri keySetUri,
		bool forceRefresh,
		CancellationToken cancellationToken)
	{
		if (!forceRefresh
			&& keySets.TryGetValue(keySetUri, out CachedKeySet? cached)
			&& cached.ExpiresAt > DateTimeOffset.UtcNow)
		{
			return cached.Keys;
		}

		await keySetLock.WaitAsync(cancellationToken);
		try
		{
			if (!forceRefresh
				&& keySets.TryGetValue(keySetUri, out cached)
				&& cached.ExpiresAt > DateTimeOffset.UtcNow)
			{
				return cached.Keys;
			}

			using HttpRequestMessage request = new(HttpMethod.Get, keySetUri);
			using HttpResponseMessage response = await httpClientFactory
				.CreateClient("NintendoJwks")
				.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			response.EnsureSuccessStatusCode();

			if (response.RequestMessage?.RequestUri is not Uri responseUri
				|| responseUri != keySetUri
				|| !TryGetTrustedKeySetUri(responseUri.AbsoluteUri, out _))
			{
				throw new SecurityTokenInvalidIssuerException("Nintendo JWK request was redirected");
			}

			string json = await response.Content.ReadAsStringAsync(cancellationToken);
			SecurityKey[] keys = [.. new JsonWebKeySet(json).GetSigningKeys()];
			if (keys.Length == 0)
				throw new SecurityTokenInvalidSigningKeyException("Nintendo JWK set is empty");

			CachedKeySet replacement = new(keys, DateTimeOffset.UtcNow.Add(KeySetLifetime));
			keySets[keySetUri] = replacement;
			return replacement.Keys;
		}
		finally
		{
			keySetLock.Release();
		}
	}

	private static bool TryGetTrustedKeySetUri(string? value, out Uri? uri)
	{
		uri = null;
		if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? candidate)
			|| candidate.Scheme != Uri.UriSchemeHttps
			|| !candidate.IsDefaultPort
			|| candidate.UserInfo.Length != 0
			|| candidate.AbsolutePath != "/1.0.0/certificates"
			|| candidate.Query.Length != 0
			|| candidate.Fragment.Length != 0)
		{
			return false;
		}

		const string suffix = ".baas.nintendo.com";
		string host = candidate.IdnHost;
		if (!host.EndsWith(suffix, StringComparison.Ordinal)
			|| host.Length != 32 + suffix.Length
			|| !host.AsSpan(0, 32).ContainsOnlyHexDigits())
		{
			return false;
		}

		uri = candidate;
		return true;
	}

	private static string? NormalizeSubject(string? subject)
		=> string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();

	private sealed record CachedKeySet(
		IReadOnlyCollection<SecurityKey> Keys,
		DateTimeOffset ExpiresAt);
}

file static class HexSpanExtensions
{
	public static bool ContainsOnlyHexDigits(this ReadOnlySpan<char> value)
	{
		foreach (char character in value)
		{
			if (!char.IsAsciiHexDigit(character))
				return false;
		}

		return true;
	}
}
