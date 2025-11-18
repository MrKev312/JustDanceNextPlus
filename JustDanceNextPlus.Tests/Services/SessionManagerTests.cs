using JustDanceNextPlus.Services;

using Moq;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace JustDanceNextPlus.Tests.Services;

public class SessionManagerTests
{
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

		Session? sessionByTicket = _sessionManager.GetSessionByNSATicket(nsaToken);
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
    public void GetSessionByNSATicket_WithValidTicket_ReturnsSession()
    {
		// Arrange
		Guid playerId = Guid.NewGuid();
		string nsaToken = "valid_token";
        _sessionManager.GenerateSession(playerId, Guid.NewGuid(), nsaToken);

		// Act
		Session? session = _sessionManager.GetSessionByNSATicket(nsaToken);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(playerId, session.PlayerId);
    }

    [Fact]
    public void GetSessionByNSATicket_WithInvalidTicket_ReturnsNull()
    {
		// Act
		Session? session = _sessionManager.GetSessionByNSATicket("invalid_token");

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
    public void GenerateOrRefreshSession_WithDifferentPlayerIds_CreatesNewSession()
    {
		// Arrange
		Guid playerId1 = Guid.NewGuid();
		Guid playerId2 = Guid.NewGuid();
		Guid appId = Guid.NewGuid();
		string nsaToken = "shared_nsa_token";

        // Create initial session with playerId1
        (_, Guid sessionId1) = _sessionManager.GenerateSession(playerId1, appId, nsaToken);

        // Act - Try to refresh with different playerId (should still refresh existing session)
        (_, Guid sessionId2) = _sessionManager.GenerateOrRefreshSession(playerId2, appId, nsaToken);

        // Assert - Should reuse the same session
        Assert.Equal(sessionId1, sessionId2);
    }

    [Fact]
    public void SessionExpirationTimeSpan_ReturnsCorrectDuration()
    {
		// Act
		TimeSpan expirationTimeSpan = _sessionManager.SessionExpirationTimeSpan;

        // Assert - Should be 3 hours
        Assert.Equal(TimeSpan.FromHours(3), expirationTimeSpan);
    }
}