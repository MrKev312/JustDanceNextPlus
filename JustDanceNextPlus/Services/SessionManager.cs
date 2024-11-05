using Microsoft.IdentityModel.Tokens;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;

namespace JustDanceNextPlus.Services;

public class SessionManager(SecurityService securityService)
{
	readonly ConcurrentDictionary<string, Guid> NSATicketToSessionId = new();

	readonly ConcurrentDictionary<Guid, Session> sessions = new();
	readonly ConcurrentDictionary<Guid, Guid> appIdToSessionId = new();

	public Session? GetSessionByNSATicket(string ticket)
	{
		if (!NSATicketToSessionId.TryGetValue(ticket, out Guid sessionId))
			return null;

		sessions.TryGetValue(sessionId, out Session? session);

		return session;
	}

	public Session? GetSessionById(Guid sessionId)
	{
		sessions.TryGetValue(sessionId, out Session? session);
		return session;
	}

	public bool TryGetSessionById(Guid sessionId, [MaybeNullWhen(false)] out Session session)
	{
		return sessions.TryGetValue(sessionId, out session);
	}

	public Session? GetSessionByAppId(Guid appId)
	{
		appIdToSessionId.TryGetValue(appId, out Guid sessionId);
		return GetSessionById(sessionId);
	}

	public (string ticket, Guid sessionId) GenerateSession(Guid playerId, Guid appId, string NSAToken)
	{
		// Generate a random session ID
		Guid sessionId;
		do
		{
			sessionId = Guid.NewGuid();
		}
		while (sessions.ContainsKey(sessionId));

		string ticket = GenerateJWTTicket(sessionId, appId);

		sessions[sessionId] = new Session { SessionId = sessionId, PlayerId = playerId, UbiAppId = appId };
		NSATicketToSessionId[NSAToken] = sessionId;
		appIdToSessionId[appId] = sessionId;

		return (ticket, sessionId);
	}

	string GenerateJWTTicket(Guid sessionId, Guid appId)
	{
		// Hardcoded header and payload values
		JwtHeader header = new(new SigningCredentials(
			new SymmetricSecurityKey(securityService.Secret256bit),
			SecurityAlgorithms.HmacSha256));

		JwtPayload payload = new()
		{
			{ "ver", "1" },
			{ "aid", appId },
			{ "env", "Prod" },
			{ "sid", sessionId },
			{ "typ", "JWT" },
			{ "enc", "A128CBC" },
			{ "int", "HS256" },
			{ "kid", "bbee8ceb-dd57-4617-9d1a-b0cc6994d2b5" }
		};

		JwtSecurityToken token = new(header, payload);

		JwtSecurityTokenHandler tokenHandler = new();
		string jwt = tokenHandler.WriteToken(token);

		return jwt;
	}
}

public class Session
{
	public required Guid SessionId { get; set; }
	public required Guid PlayerId { get; set; }
	public required Guid UbiAppId { get; set; }
}
