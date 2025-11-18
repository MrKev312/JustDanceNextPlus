using Microsoft.IdentityModel.Tokens;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;

namespace JustDanceNextPlus.Services;

public interface ISessionManager
{
	Session? GetSessionByNSATicket(string ticket);
	Session? GetSessionById(Guid sessionId);
	bool TryGetSessionById(Guid sessionId, [MaybeNullWhen(false)] out Session session);
	Session? GetSessionByAppId(Guid appId);
	(string ticket, Guid sessionId) GenerateSession(Guid playerId, Guid appId, string NSAToken);
	(string ticket, Guid sessionId) GenerateOrRefreshSession(Guid playerId, Guid appId, string NSAToken);
	IEnumerable<Session> GetAllSessions();
	TimeSpan SessionExpirationTimeSpan { get; }
	event Action? OnSessionsChanged;
}

public class SessionManager(ISecurityService securityService) : ISessionManager
{
	readonly ConcurrentDictionary<string, Guid> NSATicketToSessionId = new();

	readonly ConcurrentDictionary<Guid, Session> sessions = new();
	readonly ConcurrentDictionary<Guid, Guid> appIdToSessionId = new();
	readonly ConcurrentDictionary<Guid, Timer> sessionTimers = new();

	private const int SessionExpirationHours = 3;
	private const int SessionExpirationGraceMinutes = 10;

	public TimeSpan SessionExpirationTimeSpan => TimeSpan.FromHours(SessionExpirationHours);

	public event Action? OnSessionsChanged;

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

		DateTime expiration = DateTime.UtcNow.Add(SessionExpirationTimeSpan).Add(TimeSpan.FromMinutes(SessionExpirationGraceMinutes));
		sessions[sessionId] = new Session { SessionId = sessionId, PlayerId = playerId, UbiAppId = appId, ExpirationTime = expiration };
		NSATicketToSessionId[NSAToken] = sessionId;
		appIdToSessionId[appId] = sessionId;

		// Start expiration timer
		StartExpirationTimer(sessionId);

		OnSessionsChanged?.Invoke();

		return (ticket, sessionId);
	}

	public (string ticket, Guid sessionId) GenerateOrRefreshSession(Guid playerId, Guid appId, string NSAToken)
	{
		// Check if session already exists by NSA token
		Session? existingSession = GetSessionByNSATicket(NSAToken);
		
		if (existingSession != null)
		{
			// Refresh the existing session by resetting the expiration timer
			RefreshSession(existingSession.SessionId);
			string existingTicket = GenerateJWTTicket(existingSession.SessionId, appId);
			return (existingTicket, existingSession.SessionId);
		}

		// No existing session, generate a new one
		return GenerateSession(playerId, appId, NSAToken);
	}

	public IEnumerable<Session> GetAllSessions()
	{
		return sessions.Values;
	}

	private void StartExpirationTimer(Guid sessionId)
	{
		// Cancel existing timer if present
		if (sessionTimers.TryRemove(sessionId, out Timer? existingTimer))
		{
			existingTimer.Dispose();
		}

		// Calculate total expiration time: 3 hours + 10 minutes grace period
		TimeSpan expirationTime = TimeSpan.FromHours(SessionExpirationHours)
			.Add(TimeSpan.FromMinutes(SessionExpirationGraceMinutes));

		// Create a new timer that will remove the session when it expires
		Timer timer = new Timer(
			callback: _ => RemoveSession(sessionId),
			state: null,
			dueTime: expirationTime,
			period: Timeout.InfiniteTimeSpan);

		sessionTimers[sessionId] = timer;
	}

	private void RefreshSession(Guid sessionId)
	{
		// Restart the expiration timer
		StartExpirationTimer(sessionId);
		
		if (sessions.TryGetValue(sessionId, out Session? session))
		{
			session.ExpirationTime = DateTime.UtcNow.Add(SessionExpirationTimeSpan).Add(TimeSpan.FromMinutes(SessionExpirationGraceMinutes));
		}
	}

	private void RemoveSession(Guid sessionId)
	{
		// Remove the session from all dictionaries
		if (sessions.TryRemove(sessionId, out Session? session))
		{
			// Remove from appId mapping
			appIdToSessionId.TryRemove(session.UbiAppId, out _);

			// Remove from NSA token mapping
			string? nsaToken = NSATicketToSessionId
				.FirstOrDefault(kvp => kvp.Value == sessionId).Key;
			if (nsaToken != null)
			{
				NSATicketToSessionId.TryRemove(nsaToken, out _);
			}

			// Clean up the timer
			if (sessionTimers.TryRemove(sessionId, out Timer? timer))
			{
				timer.Dispose();
			}
		}
		
		OnSessionsChanged?.Invoke();
	}

	string GenerateJWTTicket(Guid sessionId, Guid appId)
	{
		// Hardcoded header and payload values
		JwtHeader header = new(new SigningCredentials(
			new SymmetricSecurityKey([.. securityService.Secret256bit]),
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

public record Session
{
	public required Guid SessionId { get; init; }
	public required Guid PlayerId { get; init; }
	public required Guid UbiAppId { get; init; }
	public DateTime ExpirationTime { get; set; }
}
