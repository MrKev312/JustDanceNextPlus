using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using PurchaseController = JustDanceServer.Controllers.prod_next.just_dance.com.session.v2.Session;
using PlaylistController = JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile.playlist.Played;
using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Tests.Services;
// All identities and databases are synthetic; no hosted application or map assets are used.
public sealed class AuditRegressionTests
{
    [Fact]
    public async Task EmptySessionAndPublicAppIdCannotRecoverAnotherPlayersSession()
    {
        SessionManager sessions = new(new SecurityService());
        Guid victim = Guid.NewGuid();
        (string? ticket, Guid sessionId) = sessions.GenerateSession(victim, JdNextConstants.SwitchApplicationId, "audit-victim");
        try
        {
            Mock<IBundleService> bundles = new();
            bundles.Setup(value => value.GetAllClaims()).Returns(ImmutableArray<string>.Empty);
            Mock<IPlatformTicketVerifierRegistry> verifier = new(MockBehavior.Strict);
            PurchaseController controller = new(new TimingService(), sessions, bundles.Object, verifier.Object)
            {
                ControllerContext = new() { HttpContext = new DefaultHttpContext() }
            };
            controller.Request.Headers["ubi-sessionid"] = Guid.Empty.ToString();
            controller.Request.Headers["ubi-appid"] = JdNextConstants.SwitchApplicationId.ToString();
            using JsonDocument body = JsonDocument.Parse("{}");
            Assert.IsType<NotFoundObjectResult>(await controller.PostSessions2(body.RootElement));
            verifier.VerifyNoOtherCalls();
        }
        finally
		{
			sessions.RemoveSession(sessionId);
		}
	}

	[Fact]
    public async Task BasicAuthenticationRequiresTheConfiguredPassword()
    {
        SessionLoginIdentityParser parser = CreateParser();
        using JsonDocument body = JsonDocument.Parse("{}");
		SessionLoginIdentity? first = await parser.ParseAsync(Basic("audit-login:correct"), body.RootElement, "uplay", "Player");
		SessionLoginIdentity? second = await parser.ParseAsync(Basic("audit-login:wrong"), body.RootElement, "uplay", "Player");
        Assert.NotNull(first);
        Assert.Null(second);

    }

    [Fact]
    public async Task BasicIdentityCannotAdoptLegacyNintendoProfileByPublicSubject()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        await using UserDataContext db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        Profile victim = NewProfile("Victim");
        victim.Ticket = "audit-public-nintendo-subject";
        db.Profiles.Add(victim);
        await db.SaveChangesAsync();
        using JsonDocument body = JsonDocument.Parse("{}");
        var forged = new SessionLoginIdentity("ubisoft-login", victim.Ticket, "uplay", victim.Ticket, "Attacker", victim.Ticket);
        PlayerIdentityService identities = new(db, new PlayerMetadataService(db));
		ResolvedPlayerIdentity resolved = await identities.ResolveOrCreateAsync(forged!);
        Assert.NotEqual(victim.Id, resolved.ProfileId);
        Assert.Equal("ubisoft-login", (await db.PlatformIdentities.SingleAsync()).Provider);
        Assert.Equal("Victim", (await db.Profiles.Include(p => p.Dancercard).SingleAsync(p => p.Id == victim.Id)).Dancercard.Name);
    }

    [Fact]
    public async Task AroundLeaderboardIncludesTargetAtEndOfTie()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        await using UserDataContext db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        Profile[] players = [NewProfile("A"), NewProfile("B"), NewProfile("C"), NewProfile("Z")];
        db.Profiles.AddRange(players);
        db.MapScores.AddRange(players.Select(p => new MapStats
        {
            MapName = "AuditSong", ProfileId = p.Id, HighScore = 13333, PlayCount = 1
        }));
        await db.SaveChangesAsync();
		Leaderboard result = await new LeaderboardService(db).GetLeaderboardAroundAsync("AuditSong", players[3].Id, 3);
        Assert.InRange(result.Scores.Length, 1, 3);
        Assert.Contains(result.Scores, row => row.ProfileId == players[3].Id);
        Assert.All(result.Scores, row => Assert.Equal(1, row.Position));
    }

    [Fact]
    public async Task FirstZeroScorePlaylistAcknowledgesSuccessAndStoresNoPlay()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        await using UserDataContext db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        Profile player = NewProfile("Player");
        db.Profiles.Add(player);
        await db.SaveChangesAsync();
        UserProfileService profiles = new(db, NullLogger<UserProfileService>.Instance);
        ScoreService scores = new(db, NullLogger<ScoreService>.Instance);
        UserDataService data = new(profiles, new LeaderboardService(db), scores);
        SessionManager sessions = new(new SecurityService());
        (string? ticket, Guid sessionId) = sessions.GenerateSession(player.Id, JdNextConstants.SwitchApplicationId, "audit-playlist");
        try
        {
            Guid song = Guid.NewGuid();
            string mapName = "AuditSong";
            Mock<IMapService> maps = new();
            maps.Setup(value => value.TryGetMapName(song, out mapName)).Returns(true);
            DefaultHttpContext http = new();
            http.Request.Headers["ubi-sessionid"] = sessionId.ToString();
            PlaylistController controller = new(data, maps.Object)
            {
                ControllerContext = new() { HttpContext = http }
            };
			IActionResult result = await controller.PushPlaylistPlayedAsync(new PlaylistController.PlayedWrapper
            {
                Playlist = new()
                {
                    PlaylistId = Guid.NewGuid(), TotalScore = 0,
                    MapData = [new() { MapId = song, Score = 0 }]
                }
            }, sessions, new HttpContextAccessor { HttpContext = http });
            Assert.IsType<OkObjectResult>(result);
            Assert.Empty(await db.PlaylistScores.ToListAsync());
        }
        finally
		{
			sessions.RemoveSession(sessionId);
		}
	}

	[Fact]
    public void RemovingOlderSessionPreservesNewerApplicationLookup()
    {
        SessionManager sessions = new(new SecurityService());
        (_, Guid sessionId) = sessions.GenerateSession(Guid.NewGuid(), JdNextConstants.SwitchApplicationId, "audit-first");
		(string ticket, Guid sessionId) second = sessions.GenerateSession(Guid.NewGuid(), JdNextConstants.SwitchApplicationId, "audit-second");
        try
        {
            Assert.Equal(second.sessionId, sessions.GetSessionByAppId(JdNextConstants.SwitchApplicationId)!.SessionId);
            sessions.RemoveSession(sessionId);
            Assert.NotNull(sessions.GetSessionById(second.sessionId));
            Assert.Equal(second.sessionId, sessions.GetSessionByAppId(JdNextConstants.SwitchApplicationId)!.SessionId);
        }
        finally
		{
			sessions.RemoveSession(sessionId);
			sessions.RemoveSession(second.sessionId);
		}
	}

	[Fact]
    public async Task ProfilePatchKeepsDancerCardOwnershipAndVictimCosmetics()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        await using UserDataContext db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        Profile attacker = NewProfile("Attacker");
        Profile victim = NewProfile("Victim");
        db.Profiles.AddRange(attacker, victim);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        UserDataService data = new(new UserProfileService(db, NullLogger<UserProfileService>.Instance),
            new LeaderboardService(db), new ScoreService(db, NullLogger<ScoreService>.Instance));
        SessionManager sessions = new(new SecurityService());
        (string? ticket, Guid sessionId) = sessions.GenerateSession(attacker.Id, JdNextConstants.SwitchApplicationId, "audit-patch");
        try
        {
            var controller = new JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.ProfileEndpoint(
                data, sessions, new PlayerMetadataService(db));
            IActionResult result = await controller.UpdateProfileAsync(
                new DancerCard { Id = victim.Id, Name = "Overwritten" }, sessionId.ToString());
            Assert.IsType<OkObjectResult>(result);
            db.ChangeTracker.Clear();
            Profile loadedVictim = await db.Profiles.Include(p => p.Dancercard).SingleAsync(p => p.Id == victim.Id);
            Assert.Equal("Victim", loadedVictim.Dancercard.Name);
            Profile loadedAttacker = await db.Profiles.Include(p => p.Dancercard).SingleAsync(p => p.Id == attacker.Id);
            Assert.Equal(attacker.Id, loadedAttacker.Dancercard.Id);
            Assert.Equal("Overwritten", loadedAttacker.Dancercard.Name);
        }
        finally
		{
			sessions.RemoveSession(sessionId);
		}
	}

	private static SessionLoginIdentityParser CreateParser() => new(new PlatformTicketVerifierRegistry([]),
        new BasicCredentialVerifier(Options.Create(new PlatformAuthenticationSettings
        {
            BasicPasswordHashes = new()
            {
                ["audit-login"] = new Microsoft.AspNetCore.Identity.PasswordHasher<string>()
                    .HashPassword("audit-login", "correct")
            }
        })));
    private static string Basic(string credentials) => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
    private static UserDataContext CreateDb(SqliteConnection connection) => new(
        new DbContextOptionsBuilder<UserDataContext>().UseSqlite(connection).Options);
    private static Profile NewProfile(string name)
    {
        Guid id = Guid.NewGuid();
        return new Profile { Id = id, Dancercard = new DancerCard { Id = id, Name = name } };
    }
}
