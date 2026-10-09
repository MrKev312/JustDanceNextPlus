using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;

using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace JustDanceServer.Tests.Services;

public sealed class RequestSessionResolverTests
{
	[Theory]
	[InlineData("Ubi_v1 {0}")]
	[InlineData("Ubi_v1 t={0}")]
	public void ResolvesClassicAuthorizationTicketThroughItsExistingSession(string headerFormat)
	{
		Guid sessionId = Guid.NewGuid();
		Session expected = new()
		{
			SessionId = sessionId,
			PlayerId = Guid.NewGuid(),
			UbiAppId = Guid.NewGuid()
		};
		string ticket = CreateTicket(sessionId);
		DefaultHttpContext context = new();
		context.Request.Headers.Authorization = string.Format(headerFormat, ticket);

		bool resolved = RequestSessionResolver.TryResolve(
			context.Request,
			new TestSessionManager(expected),
			out Session? actual);

		Assert.True(resolved);
		Assert.Same(expected, actual);
	}

	[Fact]
	public void RejectsAuthorizationTicketForAnUnknownSession()
	{
		string ticket = CreateTicket(Guid.NewGuid());
		DefaultHttpContext context = new();
		context.Request.Headers.Authorization = $"Ubi_v1 {ticket}";

		bool resolved = RequestSessionResolver.TryResolve(
			context.Request,
			new TestSessionManager(null),
			out Session? session);

		Assert.False(resolved);
		Assert.Null(session);
	}

	private static string CreateTicket(Guid sessionId)
		=> new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
			claims: [new Claim("sid", sessionId.ToString())]));

	private sealed class TestSessionManager(Session? session) : ISessionManager
	{
		public TimeSpan SessionExpirationTimeSpan => TimeSpan.FromHours(3);
		public Session? GetSessionById(Guid sessionId)
			=> session?.SessionId == sessionId ? session : null;
		public bool TryGetSessionById(Guid sessionId, out Session found)
		{
			found = GetSessionById(sessionId)!;
			return found is not null;
		}

		public Session? GetSessionByIdentityKey(string identityKey) => throw new NotSupportedException();
		public Session? GetSessionByAppId(Guid appId) => throw new NotSupportedException();
		public IReadOnlyCollection<Session> GetSessions() => throw new NotSupportedException();
		public IReadOnlyCollection<Session> GetSessionsByPlayerId(Guid playerId) => [];
		public bool TouchSession(Guid sessionId) => true;
		public bool RemoveSession(Guid sessionId) => false;
		public (string ticket, Guid sessionId) GenerateSession(Guid playerId, Guid appId, string nsaToken)
			=> throw new NotSupportedException();
		public (string ticket, Guid sessionId) GenerateOrRefreshSession(
			Guid playerId,
			Guid appId,
			string nsaToken) => throw new NotSupportedException();
	}
}
