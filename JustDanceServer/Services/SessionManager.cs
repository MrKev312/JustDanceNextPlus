using Microsoft.IdentityModel.Tokens;

using JustDanceServer.Configuration;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;

namespace JustDanceServer.Services;

public interface ISessionManager
{
    Session? GetSessionByIdentityKey(string identityKey);
    Session? GetSessionById(Guid sessionId);
    bool TryGetSessionById(Guid sessionId, [MaybeNullWhen(false)] out Session session);
    Session? GetSessionByAppId(Guid appId);
    IReadOnlyCollection<Session> GetSessions();
    IReadOnlyCollection<Session> GetSessionsByPlayerId(Guid playerId);
    bool TouchSession(Guid sessionId);
    bool RemoveSession(Guid sessionId);
    (string ticket, Guid sessionId) GenerateSession(Guid playerId, Guid appId, string identityKey);
    (string ticket, Guid sessionId) GenerateOrRefreshSession(Guid playerId, Guid appId, string identityKey);
    TimeSpan SessionExpirationTimeSpan { get; }
}

public class SessionManager(
    ISecurityService securityService,
    IPartyService? partyService = null,
    TimeProvider? timeProvider = null) : ISessionManager, IDisposable
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly Lock stateLock = new();
    private readonly ConcurrentDictionary<Guid, long> expirationVersions = new();
    private long nextExpirationVersion;
    readonly ConcurrentDictionary<string, Guid> identityKeyToSessionId = new();

    readonly ConcurrentDictionary<Guid, Session> sessions = new();
    readonly ConcurrentDictionary<Guid, Guid> appIdToSessionId = new();
    readonly ConcurrentDictionary<Guid, ITimer> sessionTimers = new();

    private const int SessionExpirationHours = 3;
    private const int SessionExpirationGraceMinutes = 10;

    public TimeSpan SessionExpirationTimeSpan => TimeSpan.FromHours(SessionExpirationHours);

    public Session? GetSessionByIdentityKey(string identityKey)
    {
        lock (stateLock)
        {
            if (!identityKeyToSessionId.TryGetValue(identityKey, out Guid sessionId))
                return null;

            sessions.TryGetValue(sessionId, out Session? session);

            return session;
        }
    }

    public Session? GetSessionById(Guid sessionId)
    {
        lock (stateLock)
        {
            sessions.TryGetValue(sessionId, out Session? session);
            return session;
        }
    }

    public bool TryGetSessionById(Guid sessionId, [MaybeNullWhen(false)] out Session session)
    {
        lock (stateLock)
        {
            return sessions.TryGetValue(sessionId, out session);
        }
    }

    public Session? GetSessionByAppId(Guid appId)
    {
        lock (stateLock)
        {
            appIdToSessionId.TryGetValue(appId, out Guid sessionId);
            return GetSessionById(sessionId);
        }
    }

    public IReadOnlyCollection<Session> GetSessions()
    {
        lock (stateLock)
        {
            return [.. sessions.Values.OrderByDescending(session => session.LastRefreshedAt)];
        }
    }

    public IReadOnlyCollection<Session> GetSessionsByPlayerId(Guid playerId)
        => [.. sessions.Values
            .Where(session => session.PlayerId == playerId)
            .OrderByDescending(session => session.LastRefreshedAt)];

    public bool TouchSession(Guid sessionId)
    {
        lock (stateLock)
        {
            if (!sessions.TryGetValue(sessionId, out Session? session))
                return false;

            DateTime now = clock.GetUtcNow().UtcDateTime;
            sessions[sessionId] = session with
            {
                LastRefreshedAt = now
            };
            return true;
        }
    }

    public (string ticket, Guid sessionId) GenerateSession(Guid playerId, Guid appId, string identityKey)
    {
        lock (stateLock)
        {
            // Generate a random session ID
            Guid sessionId;
            do
            {
                sessionId = Guid.NewGuid();
            }
            while (sessions.ContainsKey(sessionId));

            string ticket = GenerateJWTTicket(sessionId, appId);

            DateTime now = clock.GetUtcNow().UtcDateTime;
            sessions[sessionId] = new Session
            {
                SessionId = sessionId,
                PlayerId = playerId,
                UbiAppId = appId,
                CreatedAt = now,
                LastRefreshedAt = now,
                ExpiresAt = now.Add(SessionExpirationTimeSpan)
            };
            identityKeyToSessionId[identityKey] = sessionId;
            appIdToSessionId[appId] = sessionId;

            // Start expiration timer
            StartExpirationTimer(sessionId);

            return (ticket, sessionId);
        }
    }

    public (string ticket, Guid sessionId) GenerateOrRefreshSession(Guid playerId, Guid appId, string identityKey)
    {
        lock (stateLock)
        {
            Session? existingSession = GetSessionByIdentityKey(identityKey);

            if (existingSession != null)
            {
                if (existingSession.PlayerId != playerId)
                    throw new InvalidOperationException("A session identity cannot change its player.");
                // Refresh the existing session by resetting the expiration timer
                RefreshSession(existingSession.SessionId, appId);
                string existingTicket = GenerateJWTTicket(existingSession.SessionId, appId);
                return (existingTicket, existingSession.SessionId);
            }

            // No existing session, generate a new one
            return GenerateSession(playerId, appId, identityKey);
        }
    }

    private void StartExpirationTimer(Guid sessionId)
    {
        // Cancel existing timer if present
        if (sessionTimers.TryRemove(sessionId, out ITimer? existingTimer))
        {
            existingTimer.Dispose();
        }

        // Calculate total expiration time: 3 hours + 10 minutes grace period
        TimeSpan expirationTime = TimeSpan.FromHours(SessionExpirationHours)
            .Add(TimeSpan.FromMinutes(SessionExpirationGraceMinutes));

        // Create a new timer that will remove the session when it expires
        long version = ++nextExpirationVersion;
        expirationVersions[sessionId] = version;
        ITimer timer = clock.CreateTimer(
            callback: _ =>
            {
                lock (stateLock)
                {
                    // Disposing a timer does not cancel callbacks that are already queued.
                    if (expirationVersions.TryGetValue(sessionId, out long current) && current == version)
                        RemoveSession(sessionId);
                }
            },
            state: null,
            dueTime: expirationTime,
            period: Timeout.InfiniteTimeSpan);

        sessionTimers[sessionId] = timer;
    }

    private void RefreshSession(Guid sessionId, Guid appId)
    {
        if (sessions.TryGetValue(sessionId, out Session? session))
        {
            DateTime now = clock.GetUtcNow().UtcDateTime;
            if (session.UbiAppId != appId)
            {
                appIdToSessionId.TryRemove(new KeyValuePair<Guid, Guid>(session.UbiAppId, sessionId));
                appIdToSessionId[appId] = sessionId;
            }

            sessions[sessionId] = session with
            {
                UbiAppId = appId,
                LastRefreshedAt = now,
                ExpiresAt = now.Add(SessionExpirationTimeSpan)
            };
        }

        // Restart the expiration timer
        StartExpirationTimer(sessionId);
    }

    public bool RemoveSession(Guid sessionId)
    {
        lock (stateLock)
        {
            // Remove the session from all dictionaries
            if (sessions.TryRemove(sessionId, out Session? session))
            {
                // Remove from appId mapping
                appIdToSessionId.TryRemove(new KeyValuePair<Guid, Guid>(session.UbiAppId, sessionId));
                expirationVersions.TryRemove(sessionId, out _);

                // Remove from NSA token mapping
                string? identityKey = identityKeyToSessionId
                    .FirstOrDefault(kvp => kvp.Value == sessionId).Key;
                if (identityKey != null)
                {
                    identityKeyToSessionId.TryRemove(new KeyValuePair<string, Guid>(identityKey, sessionId));
                }

                // Clean up the timer
                if (sessionTimers.TryRemove(sessionId, out ITimer? timer))
                {
                    timer.Dispose();
                }

                if (!sessions.Values.Any(value => value.PlayerId == session.PlayerId))
                    partyService?.HandleDisconnect(session.PlayerId);
                return true;
            }

            return false;
        }
    }

    public void Dispose()
    {
        lock (stateLock)
        {
            expirationVersions.Clear();
            foreach (ITimer timer in sessionTimers.Values)
                timer.Dispose();
            sessionTimers.Clear();
            sessions.Clear();
            identityKeyToSessionId.Clear();
            appIdToSessionId.Clear();
        }

        GC.SuppressFinalize(this);
    }

    string GenerateJWTTicket(Guid sessionId, Guid appId)
    {
        // Hardcoded header and payload values
        JwtHeader header = [with(new SigningCredentials(
            new SymmetricSecurityKey([.. securityService.Secret256bit]),
            SecurityAlgorithms.HmacSha256))];

        JwtPayload payload = new()
        {
            { "ver", "1" },
            { "aid", appId },
            { "env", "Prod" },
            { "sid", sessionId },
            { "typ", "JWT" },
            { "enc", "A128CBC" },
            { "int", "HS256" },
            { "kid", JdNextConstants.SessionSigningKeyId }
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
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastRefreshedAt { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; init; } = DateTime.UtcNow.AddHours(3);
}
