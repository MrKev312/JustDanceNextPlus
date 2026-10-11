using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace JustDanceServer.Tests.Services;

public sealed class ClassicProfileServiceTests
{
    [Fact]
    public async Task SqlitePreservesTheFullUnsignedCustomizableItemIdRange()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
            .UseSqlite(connection)
            .Options;
        await using UserDataContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        Guid profileId = Guid.NewGuid();
        context.Profiles.Add(new Profile
        {
            Id = profileId,
            Dancercard = new DancerCard { Id = profileId, Name = "Player" }
        });
        context.ClassicProfiles.Add(new ClassicProfile
        {
            ProfileId = profileId,
            AvatarId = uint.MaxValue - 1,
            SkinId = uint.MaxValue - 2,
            AliasId = uint.MaxValue - 3,
            PortraitBorderId = uint.MaxValue - 4
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ClassicProfile stored = await context.ClassicProfiles.SingleAsync();
        Assert.Equal(uint.MaxValue - 1, stored.AvatarId);
        Assert.Equal(uint.MaxValue - 2, stored.SkinId);
        Assert.Equal(uint.MaxValue - 3, stored.AliasId);
        Assert.Equal(uint.MaxValue - 4, stored.PortraitBorderId);
    }

    [Fact]
    public async Task SaveProfilePreservesTheFullUnsignedCustomizableItemIdRange()
    {
        await using UserDataContext context = CreateContext();
        Guid profileId = Guid.NewGuid();
        context.Profiles.Add(new Profile
        {
            Id = profileId,
            Dancercard = new DancerCard { Id = profileId, Name = "Player" }
        });
        await context.SaveChangesAsync();
        ClassicProfileService service = new(
            context,
            Mock.Of<IScoreService>(),
            new ProgressionService(context),
            new PlayerMetadataService(context),
            NullLogger<ClassicProfileService>.Instance);
        using JsonDocument upload = JsonDocument.Parse("""
			{
			  "avatar": 4294967294,
			  "skin": 4294967293,
			  "alias": 4294967292,
			  "portraitBorder": 4294967291
			}
			""");

        Assert.True(await service.SaveProfileAsync(profileId, upload.RootElement, "wiiu"));

        ClassicProfile stored = await context.ClassicProfiles.SingleAsync();
        Assert.Equal(uint.MaxValue - 1, stored.AvatarId);
        Assert.Equal(uint.MaxValue - 2, stored.SkinId);
        Assert.Equal(uint.MaxValue - 3, stored.AliasId);
        Assert.Equal(uint.MaxValue - 4, stored.PortraitBorderId);
        JsonObject response = Assert.Single(await service.GetProfilesAsync([profileId]));
        Assert.Equal(uint.MaxValue - 1, response["avatar"]!.GetValue<uint>());
        Assert.Equal(uint.MaxValue - 2, response["skin"]!.GetValue<uint>());
        Assert.Equal(uint.MaxValue - 3, response["alias"]!.GetValue<uint>());
        Assert.Equal(uint.MaxValue - 4, response["portraitBorder"]!.GetValue<uint>());
    }

    [Fact]
    public async Task SaveProfileUnionsClientLocalUnlocksAcrossUploads()
    {
        await using UserDataContext context = CreateContext();
        Guid profileId = Guid.NewGuid();
        context.Profiles.Add(new Profile
        {
            Id = profileId,
            Dancercard = new DancerCard { Id = profileId, Name = "Player" }
        });
        context.ClassicProfiles.Add(new ClassicProfile
        {
            ProfileId = profileId,
            JdPoints = 100,
            PayloadJson = """
				{
				  "unlockedAvatars":[1,2],
				  "unlockedSkins":[4],
				  "stats":{"stars":100,"quests":9,"bestPerfectStreak":20}
				}
				"""
        });
        await context.SaveChangesAsync();
        ClassicProfileService service = new(
            context,
            Mock.Of<IScoreService>(),
            new ProgressionService(context),
            new PlayerMetadataService(context),
            NullLogger<ClassicProfileService>.Instance);
        using JsonDocument upload = JsonDocument.Parse("""
			{
			  "name":"Player",
			  "jdPoints":50,
			  "unlockedAvatars":[2,3],
			  "stats":{"stars":120,"quests":3}
			}
			""");

        Assert.True(await service.SaveProfileAsync(profileId, upload.RootElement, "switch"));

        ClassicProfile saved = await context.ClassicProfiles.SingleAsync();
        JsonObject payload = JsonNode.Parse(saved.PayloadJson)!.AsObject();
        Assert.Equal([2, 3, 1], payload["unlockedAvatars"]!.AsArray()
            .Select(value => value!.GetValue<int>()));
        Assert.Equal([4], payload["unlockedSkins"]!.AsArray()
            .Select(value => value!.GetValue<int>()));
        Assert.Equal(120, payload["stats"]!["stars"]!.GetValue<long>());
        Assert.Equal(9, payload["stats"]!["quests"]!.GetValue<long>());
        Assert.Equal(20, payload["stats"]!["bestPerfectStreak"]!.GetValue<long>());
        Assert.Equal(100, saved.JdPoints);
    }

    private static UserDataContext CreateContext()
    {
        DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UserDataContext(options);
    }
}
