using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Tests.Services;

public sealed class ProgressionServiceTests
{
	[Fact]
	public async Task UnlocksAndProgressAreIsolatedByProductFamily()
	{
		await using UserDataContext context = CreateContext();
		Profile profile = CreateProfile();
		context.Profiles.Add(profile);
		await context.SaveChangesAsync();
		ProgressionService service = new(context);

		await service.SetProgressionAsync(profile.Id, ProgressionFamily.JDPlus, 900, 5, 1);
		await service.SetProgressionAsync(profile.Id, ProgressionFamily.JDUnlimited, 40, 2, 0);
		await service.GrantUnlocksAsync(
			profile.Id,
			ProgressionFamily.JDPlus,
			["jdplus:avatar:1"],
			"test");
		await service.GrantUnlocksAsync(
			profile.Id,
			ProgressionFamily.JDUnlimited,
			["jdu:avatar:1"],
			"test");

		ProgressionSnapshot jdPlus = await service.GetAsync(profile.Id, ProgressionFamily.JDPlus);
		ProgressionSnapshot jdu = await service.GetAsync(profile.Id, ProgressionFamily.JDUnlimited);

		Assert.Equal((900, 5, 1), (jdPlus.Xp, jdPlus.Level, jdPlus.Prestige));
		Assert.Equal((40, 2, 0), (jdu.Xp, jdu.Level, jdu.Prestige));
		Assert.Equal(["jdplus:avatar:1"], jdPlus.Unlocks);
		Assert.Equal(["jdu:avatar:1"], jdu.Unlocks);
	}

	[Fact]
	public async Task ScoreProgressOnlyChangesTheSelectedFamily()
	{
		await using UserDataContext context = CreateContext();
		Profile profile = CreateProfile();
		context.Profiles.Add(profile);
		await context.SaveChangesAsync();
		ProgressionService service = new(context);

		await service.RecordMapCompletionAsync(profile.Id, ProgressionFamily.JDUnlimited, 12_000);

		ProgressionSnapshot jdPlus = await service.GetAsync(profile.Id, ProgressionFamily.JDPlus);
		ProgressionSnapshot jdu = await service.GetAsync(profile.Id, ProgressionFamily.JDUnlimited);
		Assert.Equal(1, jdPlus.Xp);
		Assert.Equal(121, jdu.Xp);
	}

	[Fact]
	public async Task QuestAndGachaStateRemainProductScoped()
	{
		await using UserDataContext context = CreateContext();
		Profile profile = CreateProfile();
		context.Profiles.Add(profile);
		await context.SaveChangesAsync();
		ProgressionService service = new(context);

		await service.SetQuestProgressAsync(
			profile.Id,
			ProgressionFamily.JDUnlimited,
			"dance-three-songs",
			3,
			completed: true,
			claimed: true);
		await service.SetGachaStateAsync(profile.Id, ProgressionFamily.JDUnlimited, 4, 12);

		Assert.Empty(await service.GetQuestProgressAsync(profile.Id, ProgressionFamily.JDPlus));
		PlayerQuestProgress jduQuest = Assert.Single(
			await service.GetQuestProgressAsync(profile.Id, ProgressionFamily.JDUnlimited));
		Assert.Equal(3, jduQuest.Progress);
		Assert.NotNull(jduQuest.CompletedAt);
		Assert.NotNull(jduQuest.ClaimedAt);
		ProgressionSnapshot jdPlus = await service.GetAsync(profile.Id, ProgressionFamily.JDPlus);
		ProgressionSnapshot jdu = await service.GetAsync(profile.Id, ProgressionFamily.JDUnlimited);
		Assert.Equal((0, 0), (jdPlus.GachaRank, jdPlus.GachaPullCount));
		Assert.Equal((4, 12), (jdu.GachaRank, jdu.GachaPullCount));
	}

	private static UserDataContext CreateContext()
	{
		DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
			.UseInMemoryDatabase($"progression-{Guid.NewGuid():N}")
			.Options;
		return new UserDataContext(options);
	}

	private static Profile CreateProfile()
	{
		Guid id = Guid.NewGuid();
		return new Profile
		{
			Id = id,
			Ticket = $"ticket-{id:N}",
			Dancercard = new DancerCard { Id = id, Name = "Player" }
		};
	}
}
