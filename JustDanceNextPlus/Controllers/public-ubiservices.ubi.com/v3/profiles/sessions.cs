using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v3.profiles;

[ApiController]
[Route("v3/profiles/sessions")]
public class Sessions(IUserDataService userDataService, ITimingService timingService, ISessionManager sessionManager) : ControllerBase
{
	[HttpPost(Name = "PostSessions")]
	public async Task<IActionResult> PostSessions([FromBody] JsonElement body)
	{
		// nameOnPlatform is optional — phone controller doesn't send it
		string nameOnPlatform = body.TryGetProperty("switch.nameOnPlatform", out JsonElement nameElem)
			? nameElem.GetString() ?? "PhoneController"
			: "PhoneController";

		// Get the Ubi-AppId from the request headers
		if (!Request.Headers.TryGetValue("Ubi-AppId", out StringValues sessionIdValues)
			|| Guid.TryParse(sessionIdValues.ToString(), out Guid ubiAppId) == false)
			return BadRequest("Missing 'Ubi-AppId' header in request");

		// Get the authentication token from the request headers
		if (!Request.Headers.TryGetValue("Authorization", out StringValues authorizationValues)
			|| authorizationValues.Count == 0)
			return BadRequest("Missing 'Authorization' header in request");

		if (!TryGetAuthenticationTicket(authorizationValues.ToString(), out string authorization))
			return BadRequest("Invalid 'Authorization' header in request");

		Profile? userData = await userDataService.GetProfileByTicketAsync(authorization);

		if (userData == null)
		{
			userData = new Profile
			{
				Ticket = authorization,
				Dancercard = new DancerCard
				{
					Name = nameOnPlatform,
				}
			};

			Guid id = await userDataService.GenerateAddProfileAsync(userData);
			userData = await userDataService.GetProfileByIdAsync(id) 
				?? throw new InvalidOperationException("Failed to retrieve newly created profile.");
		}

		// Generate or refresh session ticket
		(string ticket, Guid sessionId) = sessionManager.GenerateOrRefreshSession(userData.Id, ubiAppId, authorization);

		// Generate a new 256-bit (32 bytes) session key
		byte[] sessionKeyBytes = new byte[32];
		System.Security.Cryptography.RandomNumberGenerator.Fill(sessionKeyBytes);
		string sessionKey = Convert.ToBase64String(sessionKeyBytes);

		// Current time
		DateTime now = DateTime.UtcNow;

		// Calculate expiration time from session manager
		DateTime expiration = now.Add(sessionManager.SessionExpirationTimeSpan);

		SessionResponse response = new()
		{
			PlatformType = "uplay",
			Ticket = ticket,
			TwoFactorAuthenticationTicket = null,
			ProfileId = userData.Id,
			UserId = userData.Id,
			NameOnPlatform = userData.Dancercard.Name,
			Environment = "Prod",
			Expiration = timingService.TimeString(expiration),
			SpaceId = Guid.Parse("df67242d-9616-4652-9f40-fbcfde99b3c6"),
			ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
			ClientIpCountry = "NL",
			ServerTime = timingService.ServerTime(),
			SessionId = sessionId,
			SessionKey = sessionKey,
			RememberMeTicket = null
		};

		return Ok(response);
	}

	[HttpDelete]
	public IActionResult DeleteSession()
	{
		return Ok();
	}

	private static bool TryGetAuthenticationTicket(string authorization, out string ticket)
	{
		ticket = "";

		if (TryReadTokenScheme(authorization, "switch t=", out string switchToken))
		{
			try
			{
				JwtSecurityToken jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(switchToken);
				ticket = jwtToken.Payload.Sub ?? "";
			}
			catch (ArgumentException)
			{
				return false;
			}

			return !string.IsNullOrWhiteSpace(ticket);
		}

		if (TryReadTokenScheme(authorization, "ubi_v1 t=", out ticket)
			|| TryReadTokenScheme(authorization, "UbiMobile_v1 t=", out ticket)
			|| TryReadTokenScheme(authorization, "UbiMobile_v2 t=", out ticket)
			|| TryReadTokenScheme(authorization, "uplaypc_v1 t=", out ticket))
			return true;

		if (authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
		{
			string credentials = authorization["Basic ".Length..].Trim();
			if (credentials.Length == 0)
				return false;

			try
			{
				string decodedCredentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(credentials));
				int separatorIndex = decodedCredentials.IndexOf(':');
				ticket = separatorIndex > 0 ? decodedCredentials[..separatorIndex] : decodedCredentials;
			}
			catch (FormatException)
			{
				ticket = credentials;
			}

			return !string.IsNullOrWhiteSpace(ticket);
		}

		return false;
	}

	private static bool TryReadTokenScheme(string authorization, string prefix, out string token)
	{
		if (authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			token = authorization[prefix.Length..].Trim();
			return token.Length > 0;
		}

		token = "";
		return false;
	}
}

public class SessionResponse
{
	public string PlatformType { get; set; } = "uplay";
	public string Ticket { get; set; } = "";
	public string? TwoFactorAuthenticationTicket { get; set; }
	public Guid ProfileId { get; set; }
	public Guid UserId { get; set; }
	public string NameOnPlatform { get; set; } = "John Doe";
	public string Environment { get; set; } = "Prod";
	public string Expiration { get; set; } = "2022-01-01T00:00:00Z";
	public Guid SpaceId { get; set; }
	public string? ClientIp { get; set; }
	public string ClientIpCountry { get; set; } = "";
	public string ServerTime { get; set; } = "2022-01-01T00:00:00Z";
	public Guid SessionId { get; set; }
	public string SessionKey { get; set; } = "";
	public string? RememberMeTicket { get; set; }
}
