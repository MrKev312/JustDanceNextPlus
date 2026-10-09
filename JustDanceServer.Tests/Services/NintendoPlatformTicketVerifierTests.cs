using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JustDanceServer.Tests.Services;

public sealed class NintendoPlatformTicketVerifierTests
{
	private const string Issuer = "https://0123456789abcdef0123456789abcdef.baas.nintendo.com";
	private const string KeySetUrl = Issuer + "/1.0.0/certificates";

	[Fact]
	public async Task ValidateSubjectAsync_AcceptsNintendoHostedRs256Token()
	{
		using RSA rsa = RSA.Create(2048);
		const string keyId = "active-key";
		string token = CreateToken(rsa, keyId);
		NintendoPlatformTicketVerifier verifier = CreateVerifier(CreateJwkSet(rsa, keyId));

		VerifiedPlatformTicket? identity = await VerifyAsync(verifier, token);

		Assert.Equal("73777fcf657c42f9", identity?.Subject);
	}

	[Fact]
	public async Task ValidateSubjectAsync_RejectsWrongSignature()
	{
		using RSA signingKey = RSA.Create(2048);
		using RSA publishedKey = RSA.Create(2048);
		const string keyId = "active-key";
		NintendoPlatformTicketVerifier verifier = CreateVerifier(CreateJwkSet(publishedKey, keyId));

		Assert.Null(await VerifyAsync(verifier, CreateToken(signingKey, keyId)));
	}

	[Fact]
	public async Task ValidateSubjectAsync_RejectsWrongAudience()
	{
		using RSA rsa = RSA.Create(2048);
		const string keyId = "active-key";
		NintendoPlatformTicketVerifier verifier = CreateVerifier(CreateJwkSet(rsa, keyId));

		Assert.Null(await VerifyAsync(verifier, CreateToken(
			rsa,
			keyId,
			audience: "another-service")));
	}

	[Fact]
	public async Task ValidateSubjectAsync_RejectsTokenForAnotherNintendoTitle()
	{
		using RSA rsa = RSA.Create(2048);
		const string keyId = "active-key";
		NintendoPlatformTicketVerifier verifier = CreateVerifier(CreateJwkSet(rsa, keyId));

		Assert.Null(await VerifyAsync(verifier, CreateToken(
			rsa,
			keyId,
			applicationId: "0100bce000598000")));
	}

	[Fact]
	public async Task ValidateSubjectAsync_DoesNotTrustBodyPlatformIdOverSignedSubject()
	{
		using RSA rsa = RSA.Create(2048);
		const string keyId = "active-key";
		NintendoPlatformTicketVerifier verifier = CreateVerifier(CreateJwkSet(rsa, keyId));

		VerifiedPlatformTicket? identity = await verifier.VerifyAsync(
			"switch",
			CreateToken(rsa, keyId),
			new PlatformTicketContext(
				"switch",
				"spoofed-body-id",
				"Player",
				SwitchApplicationIds));

		Assert.Equal("73777fcf657c42f9", identity?.IdOnPlatform);
	}

	[Fact]
	public async Task ValidateSubjectAsync_RejectsNonNintendoJwkHostWithoutFetchingIt()
	{
		using RSA rsa = RSA.Create(2048);
		int requests = 0;
		NintendoPlatformTicketVerifier verifier = CreateVerifier(
			CreateJwkSet(rsa, "active-key"),
			() => requests++);
		string token = CreateToken(
			rsa,
			"active-key",
			"https://attacker.example/1.0.0/certificates");

		Assert.Null(await VerifyAsync(verifier, token));
		Assert.Equal(0, requests);
	}

	[Fact]
	public async Task ValidateSubjectAsync_DevelopmentOverrideAcceptsUnsignedToken()
	{
		JwtSecurityToken token = new(
			audience: "ed9e2f05d286f7b8",
			claims:
			[
				new Claim(JwtRegisteredClaimNames.Sub, "dev-player"),
				new Claim("typ", "id_token")
			]);
		token.Payload["nintendo"] = new Dictionary<string, string>
		{
			["ai"] = "0100a0500348a000"
		};
		NintendoPlatformTicketVerifier verifier = CreateVerifier("{\"keys\":[]}", allowUnverified: true);

		Assert.Equal(
			"dev-player",
			(await VerifyAsync(verifier, new JwtSecurityTokenHandler().WriteToken(token)))?.Subject);
	}

	[Fact]
	public async Task SessionParser_DevelopmentOverrideAcceptsPlaceholderWiiUToken()
	{
		SessionLoginIdentityParser parser = new(new PlatformTicketVerifierRegistry(
			[new DevelopmentPlatformTicketVerifier(Options.Create(
				new PlatformAuthenticationSettings
				{
					AllowUnverifiedPlatformTickets = true
				}))]));
		using JsonDocument body = JsonDocument.Parse("""
			{"nameOnPlatform":"MrBob312","idOnPlatform":1327407795}
			""");

		SessionLoginIdentity? identity = await parser.ParseAsync(
			"wiiu t=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
			body.RootElement,
			"wiiu",
			"MrBob312");

		Assert.Equal("nintendo-network", identity?.Provider);
		Assert.Equal("1327407795", identity?.Subject);
	}

	[Fact]
	public async Task NintendoDevelopmentOverrideAcceptsMalformedToken()
	{
		NintendoPlatformTicketVerifier verifier = CreateVerifier(
			"{\"keys\":[]}",
			allowUnverified: true);

		Assert.NotNull(await verifier.VerifyAsync(
			"switch",
			"not-a-jwt",
			new PlatformTicketContext("switch", null, "Player")));
	}

	[Fact]
	public async Task OpaqueTicketsAreRejectedWithoutDevelopmentOverride()
	{
		DevelopmentPlatformTicketVerifier verifier = new(Options.Create(
			new PlatformAuthenticationSettings()));

		Assert.Null(await verifier.VerifyAsync(
			"wiiu",
			"c768ad1e29b04f3280a64d7c441f07637d9f6d957cb14ac69df92de409a0f02e",
			new PlatformTicketContext("wiiu", "1327407795", "Player")));
	}

	[Fact]
	public async Task DevelopmentOverrideDoesNotInspectPsnToken()
	{
		DevelopmentPlatformTicketVerifier verifier = CreateDevelopmentVerifier();

		Assert.NotNull(await verifier.VerifyAsync(
			"psn2.0",
			"not-base64-or-an-authorization-code",
			new PlatformTicketContext("psn", null, "Player")));
	}

	[Fact]
	public async Task DevelopmentOverrideDoesNotInspectXboxToken()
	{
		DevelopmentPlatformTicketVerifier verifier = CreateDevelopmentVerifier();

		Assert.NotNull(await verifier.VerifyAsync(
			"XBL3.0",
			"anything",
			new PlatformTicketContext("xbl", "123456789", "Player")));
	}

	[Fact]
	public async Task SessionParserAcceptsXboxHeaderWithoutTicketMarkerInDevelopment()
	{
		SessionLoginIdentityParser parser = new(new PlatformTicketVerifierRegistry(
			[CreateDevelopmentVerifier()]));
		using JsonDocument body = JsonDocument.Parse("{\"idOnPlatform\":\"123456789\"}");

		SessionLoginIdentity? identity = await parser.ParseAsync(
			"XBL3.0 anything",
			body.RootElement,
			"xbl",
			"Player");

		Assert.Equal("xbox-network", identity?.Provider);
		Assert.Equal("123456789", identity?.Subject);
	}

	[Fact]
	public async Task RegistryCanAddAnotherPlatformWithoutChangingTheParser()
	{
		SessionLoginIdentityParser parser = new(new PlatformTicketVerifierRegistry(
			[new TestSteamTicketVerifier()]));
		using JsonDocument body = JsonDocument.Parse("{}");

		SessionLoginIdentity? identity = await parser.ParseAsync(
			"steam t=valid-steam-ticket",
			body.RootElement,
			"steam",
			"Steam Player");

		Assert.Equal("steam", identity?.Provider);
		Assert.Equal("steam-account-id", identity?.Subject);
	}

	private static NintendoPlatformTicketVerifier CreateVerifier(
		string jwkSet,
		Action? onRequest = null,
		bool allowUnverified = false)
	{
		HttpClient client = new(new StaticResponseHandler(jwkSet, onRequest));
		return new NintendoPlatformTicketVerifier(
			new SingleHttpClientFactory(client),
			Options.Create(new PlatformAuthenticationSettings
			{
				AllowUnverifiedPlatformTickets = allowUnverified
			}),
			NullLogger<NintendoPlatformTicketVerifier>.Instance);
	}

	private static Task<VerifiedPlatformTicket?> VerifyAsync(
		NintendoPlatformTicketVerifier verifier,
		string ticket)
		=> verifier.VerifyAsync(
			"switch",
			ticket,
			new PlatformTicketContext("switch", null, "Player", SwitchApplicationIds));

	private static string CreateToken(
		RSA rsa,
		string keyId,
		string keySetUrl = KeySetUrl,
		string audience = "ed9e2f05d286f7b8",
		string applicationId = "0100a0500348a000")
	{
		RsaSecurityKey securityKey = new(rsa) { KeyId = keyId };
		SigningCredentials credentials = new(securityKey, SecurityAlgorithms.RsaSha256);
		JwtSecurityToken token = new(
			issuer: new Uri(keySetUrl).GetLeftPart(UriPartial.Authority),
			audience: audience,
			claims:
			[
				new Claim(JwtRegisteredClaimNames.Sub, "73777fcf657c42f9"),
				new Claim("typ", "id_token")
			],
			notBefore: DateTime.UtcNow.AddMinutes(-1),
			expires: DateTime.UtcNow.AddMinutes(10),
			signingCredentials: credentials);
		token.Payload["nintendo"] = new Dictionary<string, string>
		{
			["ai"] = applicationId
		};
		token.Header["jku"] = keySetUrl;
		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private static DevelopmentPlatformTicketVerifier CreateDevelopmentVerifier()
		=> new(Options.Create(new PlatformAuthenticationSettings
		{
			AllowUnverifiedPlatformTickets = true
		}));

	private static IReadOnlyList<string> SwitchApplicationIds { get; } =
		["0100a0500348a000"];

	private static string CreateJwkSet(RSA rsa, string keyId)
	{
		RSAParameters parameters = rsa.ExportParameters(false);
		return JsonSerializer.Serialize(new
		{
			keys = new[]
			{
				new
				{
					kty = "RSA",
					use = "sig",
					alg = "RS256",
					kid = keyId,
					n = Base64UrlEncoder.Encode(parameters.Modulus),
					e = Base64UrlEncoder.Encode(parameters.Exponent)
				}
			}
		});
	}

	private sealed class StaticResponseHandler(string content, Action? onRequest) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			onRequest?.Invoke();
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				RequestMessage = request,
				Content = new StringContent(content, Encoding.UTF8, "application/json")
			});
		}
	}

	private sealed class SingleHttpClientFactory(HttpClient client) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => client;
	}

	private sealed class TestSteamTicketVerifier : IPlatformTicketVerifier
	{
		public IReadOnlySet<string> AuthorizationSchemes { get; } =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "steam" };

		public Task<VerifiedPlatformTicket?> VerifyAsync(
			string authorizationScheme,
			string ticket,
			PlatformTicketContext context,
			CancellationToken cancellationToken = default)
			=> Task.FromResult<VerifiedPlatformTicket?>(ticket == "valid-steam-ticket"
				? new("steam", "steam-account-id", "steam-account-id")
				: null);
	}
}
