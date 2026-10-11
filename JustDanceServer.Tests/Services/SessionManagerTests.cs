using JustDanceServer.Services;

using Moq;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace JustDanceServer.Tests.Services;

public class SessionManagerTests : IDisposable
{
    public void Dispose()
    {
        _sessionManager.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ConcurrentLoginsForOneIdentityShareOneSession()
    {
        Guid player = Guid.NewGuid(), app = Guid.NewGuid();
        (string ticket, Guid sessionId)[] results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
            _sessionManager.GenerateOrRefreshSession(player, app, "concurrent"))));
        Assert.Single(results.Select(result => result.sessionId).Distinct());
        Assert.Single(_sessionManager.GetSessions());
    }

    [Fact]
    public void QueuedOldExpirationCannotDeleteARefreshedSession()
    {
        ManualTimerProvider clock = new();
        using SessionManager sessions = new(_mockSecurityService.Object, timeProvider: clock);
        Guid player = Guid.NewGuid(), app = Guid.NewGuid();
        (string? ticket, Guid sessionId) = sessions.GenerateOrRefreshSession(player, app, "refresh");
        Action oldCallback = clock.Callbacks[0];
        sessions.GenerateOrRefreshSession(player, app, "refresh");
        oldCallback();
        Assert.NotNull(sessions.GetSessionById(sessionId));
        clock.Callbacks[1]();
        Assert.Null(sessions.GetSessionById(sessionId));
        Assert.Null(sessions.GetSessionByIdentityKey("refresh"));
    }

    private sealed class ManualTimerProvider : TimeProvider
    {
        public List<Action> Callbacks { get; } = [];
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Callbacks.Add(() => callback(state));
            return new ManualTimer();
        }
        private sealed class ManualTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly SessionManager _sessionManager;
    private readonly byte[] _testSecretKey;

    public SessionManagerTests()
    {
        _mockSecurityService = new Mock<ISecurityService>();

        // Use a consistent 256-bit (32-byte) key for predictable JWT generation in tests.
        _testSecretKey = new byte[32];
        RandomNumberGenerator.Fill(_testSecretKey);

        _mockSecurityService.Setup(s => s.Secret256bit).Returns(_testSecretKey);

        _sessionManager = new SessionManager(_mockSecurityService.Object);
    }

    [Fact]
    public void GenerateSession_CreatesAndStoresSessionCorrectly()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();
        Guid appId = Guid.NewGuid();
        string nsaToken = "test_nsa_token";

        // Act
        (string ticket, Guid sessionId) = _sessionManager.GenerateSession(playerId, appId, nsaToken);

        // Assert
        Assert.False(string.IsNullOrEmpty(ticket));
        Assert.NotEqual(Guid.Empty, sessionId);

        Session? sessionById = _sessionManager.GetSessionById(sessionId);
        Assert.NotNull(sessionById);
        Assert.Equal(playerId, sessionById.PlayerId);
        Assert.Equal(appId, sessionById.UbiAppId);

        Session? sessionByTicket = _sessionManager.GetSessionByIdentityKey(nsaToken);
        Assert.Equal(sessionById, sessionByTicket);

        Session? sessionByAppId = _sessionManager.GetSessionByAppId(appId);
        Assert.Equal(sessionById, sessionByAppId);
    }

    [Fact]
    public void GenerateSession_ReturnsValidJwtTicketWithCorrectClaims()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();
        Guid appId = Guid.NewGuid();

        // Act
        (string ticket, Guid sessionId) = _sessionManager.GenerateSession(playerId, appId, "any_token");
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken decodedToken = handler.ReadJwtToken(ticket);

        // Assert
        string? sidClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == "sid")?.Value;
        string? aidClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == "aid")?.Value;

        Assert.NotNull(sidClaim);
        Assert.Equal(sessionId.ToString(), sidClaim);
        Assert.NotNull(aidClaim);
        Assert.Equal(appId.ToString(), aidClaim);
    }

    [Fact]
    public void GetSessionByIdentityKey_WithValidKey_ReturnsSession()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();
        string nsaToken = "valid_token";
        _sessionManager.GenerateSession(playerId, Guid.NewGuid(), nsaToken);

        // Act
        Session? session = _sessionManager.GetSessionByIdentityKey(nsaToken);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(playerId, session.PlayerId);
    }

    [Fact]
    public void GetSessionByIdentityKey_WithInvalidKey_ReturnsNull()
    {
        // Act
        Session? session = _sessionManager.GetSessionByIdentityKey("invalid_token");

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public void GetSessionById_WithValidId_ReturnsSession()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();
        (_, Guid sessionId) = _sessionManager.GenerateSession(playerId, Guid.NewGuid(), "any_token");

        // Act
        Session? session = _sessionManager.GetSessionById(sessionId);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(playerId, session.PlayerId);
    }

    [Fact]
    public void GetSessionById_WithInvalidId_ReturnsNull()
    {
        // Act
        Session? session = _sessionManager.GetSessionById(Guid.NewGuid());

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public void TryGetSessionById_WithValidId_ReturnsTrueAndSession()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();
        (_, Guid sessionId) = _sessionManager.GenerateSession(playerId, Guid.NewGuid(), "any_token");

        // Act
        bool success = _sessionManager.TryGetSessionById(sessionId, out Session? session);

        // Assert
        Assert.True(success);
        Assert.NotNull(session);
        Assert.Equal(playerId, session.PlayerId);
    }

    [Fact]
    public void TryGetSessionById_WithInvalidId_ReturnsFalseAndNull()
    {
        // Act
        bool success = _sessionManager.TryGetSessionById(Guid.NewGuid(), out Session? session);

        // Assert
        Assert.False(success);
        Assert.Null(session);
    }

    [Fact]
    public void GetSessionByAppId_WithValidAppId_ReturnsSession()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();
        Guid appId = Guid.NewGuid();
        _sessionManager.GenerateSession(playerId, appId, "any_token");

        // Act
        Session? session = _sessionManager.GetSessionByAppId(appId);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(playerId, session.PlayerId);
    }

    [Fact]
    public void GetSessionByAppId_WithInvalidAppId_ReturnsNull()
    {
        // Act
        Session? session = _sessionManager.GetSessionByAppId(Guid.NewGuid());

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public void GenerateOrRefreshSession_WithNewToken_CreatesNewSession()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();
        Guid appId = Guid.NewGuid();
        string nsaToken = "new_nsa_token";

        // Act
        (string ticket, Guid sessionId) = _sessionManager.GenerateOrRefreshSession(playerId, appId, nsaToken);

        // Assert
        Assert.False(string.IsNullOrEmpty(ticket));
        Assert.NotEqual(Guid.Empty, sessionId);

        Session? session = _sessionManager.GetSessionById(sessionId);
        Assert.NotNull(session);
        Assert.Equal(playerId, session.PlayerId);
        Assert.Equal(appId, session.UbiAppId);
    }

    [Fact]
    public void GenerateOrRefreshSession_WithExistingToken_RefreshesExistingSession()
    {
        // Arrange
        Guid playerId = Guid.NewGuid();
        Guid appId = Guid.NewGuid();
        string nsaToken = "existing_nsa_token";

        // Create initial session
        (_, Guid originalSessionId) = _sessionManager.GenerateSession(playerId, appId, nsaToken);

        // Act - Refresh the session with the same token
        (string refreshedTicket, Guid refreshedSessionId) = _sessionManager.GenerateOrRefreshSession(playerId, appId, nsaToken);

        // Assert
        Assert.Equal(originalSessionId, refreshedSessionId);
        Assert.False(string.IsNullOrEmpty(refreshedTicket));

        Session? session = _sessionManager.GetSessionById(refreshedSessionId);
        Assert.NotNull(session);
        Assert.Equal(playerId, session.PlayerId);
    }

    [Fact]
    public void GenerateOrRefreshSession_WithDifferentPlayerIds_RejectsIdentityChange()
    {
        // Arrange
        Guid playerId1 = Guid.NewGuid();
        Guid playerId2 = Guid.NewGuid();
        Guid appId = Guid.NewGuid();
        string nsaToken = "shared_nsa_token";

        // Create initial session with playerId1
        (_, Guid sessionId1) = _sessionManager.GenerateSession(playerId1, appId, nsaToken);

        Assert.Throws<InvalidOperationException>(() =>
            _sessionManager.GenerateOrRefreshSession(playerId2, appId, nsaToken));
        Assert.Equal(playerId1, _sessionManager.GetSessionById(sessionId1)!.PlayerId);
    }

    [Fact]
    public void SessionExpirationTimeSpan_ReturnsCorrectDuration()
    {
        // Act
        TimeSpan expirationTimeSpan = _sessionManager.SessionExpirationTimeSpan;

        // Assert - Should be 3 hours
        Assert.Equal(TimeSpan.FromHours(3), expirationTimeSpan);
    }

    [Fact]
    public void RemoveSessionNotifiesPartyAfterThePlayersLastConnectionCloses()
    {
        Mock<IPartyService> parties = new();
        SessionManager sessions = new(_mockSecurityService.Object, parties.Object);
        Guid playerId = Guid.NewGuid();
        (_, Guid first) = sessions.GenerateSession(playerId, Guid.NewGuid(), "identity-one");
        (_, Guid second) = sessions.GenerateSession(playerId, Guid.NewGuid(), "identity-two");

        Assert.True(sessions.RemoveSession(first));
        parties.Verify(value => value.HandleDisconnect(playerId), Times.Never);
        Assert.True(sessions.RemoveSession(second));
        parties.Verify(value => value.HandleDisconnect(playerId), Times.Once);
    }
}
