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
		string? nameOnPlatform = body.GetProperty("switch.nameOnPlatform").GetString();

		if (nameOnPlatform == null)
			return BadRequest("Missing 'switch.nameOnPlatform' field in request body");

		// Get the Ubi-AppId from the request headers
		if (!Request.Headers.TryGetValue("Ubi-AppId", out StringValues sessionIdValues)
			|| Guid.TryParse(sessionIdValues.ToString(), out Guid ubiAppId) == false)
			return BadRequest("Missing 'Ubi-AppId' header in request");

		// Get the authentication token from the request headers
		if (!Request.Headers.TryGetValue("Authorization", out StringValues authorizationValues)
			|| authorizationValues.Count == 0)
			return BadRequest("Missing 'Authorization' header in request");

		string authorization = authorizationValues.ToString();
		if (!authorization.StartsWith("switch t="))
			return BadRequest("Invalid 'Authorization' header in request");
		authorization = authorization[9..];

		// Convert to JWT token
		JwtSecurityToken jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(authorization);
		// From the payload, get the subject (sub) claim
		string? subject = jwtToken.Payload.Sub;
		if (subject == null)
			return BadRequest("Invalid 'Authorization' header in request");
		authorization = subject;

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

		// Generate a session ticket
		(string ticket, Guid sessionId) = sessionManager.GenerateSession(userData.Id, ubiAppId, authorization);

		// Generate a new 256-bit (32 bytes) session key
		byte[] sessionKeyBytes = new byte[32];
		System.Security.Cryptography.RandomNumberGenerator.Fill(sessionKeyBytes);
		string sessionKey = Convert.ToBase64String(sessionKeyBytes);

		// Current time
		DateTime now = DateTime.UtcNow;

		// Time in 3 hours
		DateTime expiration = now.AddHours(3);

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
