using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using System.Text.Json;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.profiles;

[ApiController]
[Route("v2/profiles/sessions")]
[Route("v3/profiles/sessions")]
public class Sessions(
    IPlayerIdentityService playerIdentityService,
    ISessionLoginIdentityParser loginIdentityParser,
    ITimingService timingService,
    ISessionManager sessionManager) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostSessions([FromBody] JsonElement body)
    {
        // nameOnPlatform is optional — phone controller doesn't send it. Switch uses
        // a platform-qualified property while the older console clients use the plain one.
        string nameOnPlatform = GetOptionalString(body, "nameOnPlatform")
            ?? GetOptionalString(body, "switch.nameOnPlatform")
            ?? "PhoneController";

        // Get the Ubi-AppId from the request headers
        if (!Request.Headers.TryGetValue("Ubi-AppId", out StringValues sessionIdValues)
            || Guid.TryParse(sessionIdValues.ToString(), out Guid ubiAppId) == false)
            return BadRequest("Missing 'Ubi-AppId' header in request");

        if (!UbisoftApplicationProfiles.TryGetSessionIdentity(
            ubiAppId,
            out UbisoftSessionIdentity? sessionIdentity))
        {
            return BadRequest("Unsupported 'Ubi-AppId' header in request");
        }

        // Get the authentication token from the request headers
        if (!Request.Headers.TryGetValue("Authorization", out StringValues authorizationValues)
            || authorizationValues.Count == 0)
            return BadRequest("Missing 'Authorization' header in request");

        SessionLoginIdentity? loginIdentity = await loginIdentityParser.ParseAsync(
            authorizationValues.ToString(),
            body,
            sessionIdentity.PlatformType,
            nameOnPlatform,
            sessionIdentity.PlatformApplicationIds,
            HttpContext.RequestAborted);
        if (loginIdentity is null)
            return BadRequest("Invalid 'Authorization' header in request");

        ResolvedPlayerIdentity player = await playerIdentityService.ResolveOrCreateAsync(loginIdentity);

        // Generate or refresh session ticket
        string applicationSessionKey = SessionIdentityKey.ForApplication(player.SessionKey, ubiAppId);
        (string ticket, Guid sessionId) = sessionManager.GenerateOrRefreshSession(
            player.ProfileId,
            ubiAppId,
            applicationSessionKey);

        // Captured UbiServices session responses use a 512-bit (64-byte) key.
        byte[] sessionKeyBytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(sessionKeyBytes);
        string sessionKey = Convert.ToBase64String(sessionKeyBytes);

        // Current time
        DateTime now = DateTime.UtcNow;

        // Calculate expiration time from session manager
        DateTime expiration = now.Add(sessionManager.SessionExpirationTimeSpan);

        SessionResponse response = new()
        {
            PlatformType = sessionIdentity.PlatformType,
            Ticket = ticket,
            TwoFactorAuthenticationTicket = null,
            ProfileId = player.ProfileId,
            UserId = player.ProfileId,
            NameOnPlatform = player.NameOnPlatform,
            Environment = "Prod",
            Expiration = timingService.TimeString(expiration),
            SpaceId = sessionIdentity.SpaceId,
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
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();

        return sessionManager.RemoveSession(session.SessionId) ? Ok() : NotFound();
    }

    private static string? GetOptionalString(JsonElement body, string propertyName)
    {
        if (!body.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
            return null;

        string? value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
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
    public bool HasAcceptedLegalOptins { get; set; } = true;
    public string Environment { get; set; } = "Prod";
    public string Expiration { get; set; } = "2022-01-01T00:00:00Z";
    public Guid SpaceId { get; set; }
    public string? ClientIp { get; set; }
    public string ClientIpCountry { get; set; } = "";
    public string ServerTime { get; set; } = "2022-01-01T00:00:00Z";
    public Guid SessionId { get; set; }
    public object[] AccountIssues { get; set; } = [];
    public string SessionKey { get; set; } = "";
    public string? RememberMeTicket { get; set; }
}
