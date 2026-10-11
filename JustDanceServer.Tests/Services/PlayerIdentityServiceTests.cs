using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.EntityFrameworkCore;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace JustDanceServer.Tests.Services;

public sealed class PlayerIdentityServiceTests
{
    [Fact]
    public async Task SwitchSubjectLinksJduAndJdPlusToOnePlayer()
    {
        await using UserDataContext context = CreateContext();
        PlayerIdentityService service = new(context, new PlayerMetadataService(context));

        SessionLoginIdentity jduLogin = await ParseSwitchLogin(
            CreateSwitchToken("73777fcf657c42f9", "0100a0500348a000"),
            "JDU Player");
        SessionLoginIdentity jdPlusLogin = await ParseSwitchLogin(
            CreateSwitchToken("73777fcf657c42f9", "0100c2500fc20000"),
            "JD+ Player");

        ResolvedPlayerIdentity jdu = await service.ResolveOrCreateAsync(jduLogin);
        ResolvedPlayerIdentity jdPlus = await service.ResolveOrCreateAsync(jdPlusLogin);

        Assert.Equal(jdu.ProfileId, jdPlus.ProfileId);
        Assert.Equal("nintendo-account:73777fcf657c42f9", jdPlus.SessionKey);
        Assert.Equal(1, await context.Profiles.CountAsync());
        PlatformIdentity identity = await context.PlatformIdentities.SingleAsync();
        Assert.Equal(jdu.ProfileId, identity.ProfileId);
        Assert.Equal("switch", identity.PlatformType);
        Assert.Equal("73777fcf657c42f9", identity.IdOnPlatform);

        IReadOnlyList<LinkedProfileIdentity> linked =
            await service.GetLinkedProfilesAsync(jdu.ProfileId);
        Assert.Contains(linked, profile =>
            profile.PlatformType == "switch"
            && profile.IdOnPlatform == "73777fcf657c42f9");
        Assert.Contains(linked, profile =>
            profile.PlatformType == "uplay"
            && profile.ProfileId == jdu.ProfileId);
    }

    [Fact]
    public async Task ExistingTicketProfileIsAdoptedWithoutLosingItsId()
    {
        await using UserDataContext context = CreateContext();
        Guid profileId = Guid.NewGuid();
        context.Profiles.Add(new Profile
        {
            Id = profileId,
            Ticket = "73777fcf657c42f9",
            Dancercard = new DancerCard { Id = profileId, Name = "Old Player" }
        });
        await context.SaveChangesAsync();

        PlayerIdentityService service = new(context, new PlayerMetadataService(context));
        ResolvedPlayerIdentity resolved = await service.ResolveOrCreateAsync(await ParseSwitchLogin(
            CreateSwitchToken("73777fcf657c42f9", "0100c2500fc20000"),
            "Current Player"));

        Assert.Equal(profileId, resolved.ProfileId);
        Assert.Equal(1, await context.Profiles.CountAsync());
        Assert.Equal(profileId, (await context.PlatformIdentities.SingleAsync()).ProfileId);
    }

    private static UserDataContext CreateContext()
    {
        DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UserDataContext(options);
    }

    private static async Task<SessionLoginIdentity> ParseSwitchLogin(string token, string name)
    {
        using JsonDocument body = JsonDocument.Parse("{}");
        SessionLoginIdentityParser parser = new(new PlatformTicketVerifierRegistry(
            [new TestNintendoTicketVerifier()]));
        SessionLoginIdentity? login = await parser.ParseAsync(
            $"switch t={token}",
            body.RootElement,
            "switch",
            name);
        return Assert.IsType<SessionLoginIdentity>(login);
    }

    private static string CreateSwitchToken(string subject, string applicationId)
    {
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, subject),
            new("nintendo.ai", applicationId)
        ];
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: claims));
    }

    private sealed class TestNintendoTicketVerifier : IPlatformTicketVerifier
    {
        public IReadOnlySet<string> AuthorizationSchemes { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "switch" };

        public Task<VerifiedPlatformTicket?> VerifyAsync(
            string authorizationScheme,
            string encodedToken,
            PlatformTicketContext context,
            CancellationToken cancellationToken = default)
        {
            string? subject = new JwtSecurityTokenHandler().ReadJwtToken(encodedToken).Subject;
            return Task.FromResult<VerifiedPlatformTicket?>(subject is null
                ? null
                : new("nintendo-account", subject, subject));
        }
    }
}
