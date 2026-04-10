using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.session.v2;

[ApiController]
[Route("session/v2")]
public class Session(ITimingService timingService, ISessionManager sessionManager, IBundleService bundleService) : ControllerBase
{
	[HttpPost("session")]
	[HttpPost("refresh-purchase")]
	public IActionResult PostSessions2([FromBody] JsonElement body)
	{
		Console.WriteLine($"[session/v2/session] body: {body}");

		// Guard: if body is not an object, skip all property access
		Guid bootID = Guid.NewGuid();
		string? nsaIdToken = null;

		if (body.ValueKind == JsonValueKind.Object)
		{
			if (body.TryGetProperty("bootId", out JsonElement bootIdElement)
				&& Guid.TryParse(bootIdElement.GetString(), out bootID))
			{
				// Switch flow: bootId provided
			}

			// Try to get nsaIdToken (Switch flow) — optional for phone controller
			if (body.TryGetProperty("userOwnership", out JsonElement userOwnerShipElement)
				&& userOwnerShipElement.ValueKind == JsonValueKind.Object
				&& userOwnerShipElement.TryGetProperty("dlcCheckToken", out JsonElement dlcCheckTokenElement)
				&& dlcCheckTokenElement.ValueKind == JsonValueKind.Object
				&& dlcCheckTokenElement.TryGetProperty("nsaIdToken", out JsonElement nsaIdTokenElement))
			{
				nsaIdToken = nsaIdTokenElement.GetString();
			}
		}

		// Get the ubi-sessionid from the request headers
		if (!Request.Headers.TryGetValue("ubi-sessionid", out Microsoft.Extensions.Primitives.StringValues sessionIdValues)
			|| Guid.TryParse(sessionIdValues.ToString(), out Guid sessionId) == false)
		{
			return BadRequest("Missing 'ubi-sessionid' header in request");
		}

		Services.Session? session;
		if (nsaIdToken != null)
		{
			// Switch flow: parse JWT to get subject
			JwtSecurityToken jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(nsaIdToken);
			string? subject = jwtToken.Payload.Sub;
			if (subject == null)
				return BadRequest("Invalid 'userOwnership.dlcCheckToken.nsaIdToken' field in request body");

			session = sessionId == Guid.Empty
				? sessionManager.GetSessionByNSATicket(subject)
				: sessionManager.GetSessionById(sessionId);
		}
		else if (sessionId != Guid.Empty)
		{
			session = sessionManager.GetSessionById(sessionId);
		}
		else
		{
			// Phone controller flow: sessionId is empty, find session by ubi-appid
			session = null;
			if (Request.Headers.TryGetValue("ubi-appid", out var appIdValues)
				&& Guid.TryParse(appIdValues.ToString(), out Guid appId))
			{
				session = sessionManager.GetSessionByAppId(appId);
			}
		}

		if (session == null)
			return NotFound("Session not found");

        IReadOnlyList<string> claims = bundleService.GetAllClaims();

		PurchaseResponse purchaseResponse = new()
		{
			SessionId = session.SessionId.ToString(),
			BootId = bootID.ToString(),
			ProfileId = session.PlayerId.ToString(),
			DurationSeconds = 10745,
			// Todo: update this with a packService?
			SubscriptionInfo = new()
			{
				Dlcs = new()
				{
					Owned = [.. claims.AsEnumerable().Select((claim, index) => new Dlc(index.ToString(), [new(claim)]))]
				},
				Claims =
				[
					new("jdplus"),
					new("welcomeGifts"),
					.. claims.Select(claim => new Claim(claim))
				],
				Subscriptions = new()
				{
					NsaId = "c263224b2e53e318",
					Hm = false,
					Owned =
					[
						new()
						{
							Expiration = DateTimeOffset.UtcNow.AddDays(6968.5).ToUnixTimeSeconds(),
							Claims =
							[
								new("jdplus"),
								new("welcomeGifts")
							],
							Activation = 0,
							Id = "0"
						}
					]
				},
				Events = new()
				{
					Activated = [],
					Blocked = [],
					ToActivate = []
				}
			},
			DeviceId = "64ea34418b9237649ded944a03d89b48",
			ServerTime = timingService.ServerTime()
		};

		return Ok(purchaseResponse);
	}
}

public class PurchaseResponse
{
	public required string SessionId { get; set; }
	public required string BootId { get; set; }
	public required string ProfileId { get; set; }
	public int DurationSeconds { get; set; }
	public SubscriptionInfo SubscriptionInfo { get; set; } = new();
	public string DeviceId { get; set; } = "";
	public required string ServerTime { get; set; }
}

public class SubscriptionInfo
{
	public Dlcs Dlcs { get; set; } = new();
	public List<Claim> Claims { get; set; } = [];
	public Subscriptions Subscriptions { get; set; } = new();
	public Events Events { get; set; } = new();
}

public class Dlcs
{
	public List<Dlc> Owned { get; set; } = [];
}

public class Dlc(string id, List<Claim> claims)
{
	public string Id { get; set; } = id;
	public List<Claim> Claims { get; set; } = claims;
}

public class Claim(string id)
{
	public string Id { get; set; } = id;
}

public class Subscriptions
{
	public string NsaId { get; set; } = "";
	public bool Hm { get; set; }
	public List<Subscription> Owned { get; set; } = [];
}

public class Subscription
{
	public long Expiration { get; set; }
	public List<Claim> Claims { get; set; } = [];
	public int Activation { get; set; }
	public string Id { get; set; } = "";
}

public class Events
{
	public List<object> Activated { get; set; } = [];
	public List<object> Blocked { get; set; } = [];
	public List<object> ToActivate { get; set; } = [];
}