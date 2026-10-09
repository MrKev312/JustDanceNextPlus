using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustDanceServer.Tests.Services;

public sealed class ChallengeMatchServiceTests : IAsyncDisposable
{
	private readonly SqliteConnection connection = new("Data Source=:memory:;Pooling=False");

	[Fact]
	public async Task ChallengeRequiresAnExistingScoreFromAnotherProfile()
	{
		await using ServiceProvider provider = await CreateProviderAsync();
		IChallengeMatchService service = provider.GetRequiredService<IChallengeMatchService>();
		Guid requester = Guid.NewGuid();

		Assert.Null(await service.FindChallengeForMapAsync(requester, "PokerFaceALT"));

		await using (AsyncServiceScope scope = provider.CreateAsyncScope())
		{
			UserDataContext context = scope.ServiceProvider.GetRequiredService<UserDataContext>();
			context.MapScores.Add(new MapStats
			{
				ProfileId = requester,
				MapName = "PokerFaceALT",
				HighScore = 11_000
			});
			await context.SaveChangesAsync();
		}

		Assert.Null(await service.FindChallengeForMapAsync(requester, "PokerFaceALT"));
	}

	[Fact]
	public async Task FinalizedChallengeWritesIntoTheSharedMapScoreTable()
	{
		await using ServiceProvider provider = await CreateProviderAsync();
		IChallengeMatchService service = provider.GetRequiredService<IChallengeMatchService>();
		Guid requester = Guid.NewGuid();
		Guid opponent = Guid.NewGuid();

		await using (AsyncServiceScope scope = provider.CreateAsyncScope())
		{
			UserDataContext context = scope.ServiceProvider.GetRequiredService<UserDataContext>();
			context.Profiles.Add(new Profile { Id = requester });
			context.MapScores.Add(new MapStats
			{
				ProfileId = opponent,
				MapName = "PokerFaceALT",
				HighScore = 10_000
			});
			await context.SaveChangesAsync();
		}

		Guid ugcId = Assert.IsType<Guid>(await service.FindChallengeForMapAsync(requester, "PokerFaceALT"));
		ChallengeMatchInfo created = Assert.IsType<ChallengeMatchInfo>(await service.CreateAsync(
			requester,
			ChallengeMatchType.Friendly,
			opponent,
			ugcId));
		Assert.NotNull(service.StartRound(created.MatchId, requester));
		ChallengeMatchInfo completed = Assert.IsType<ChallengeMatchInfo>(await service.FinalizeRoundAsync(
			created.MatchId,
			requester,
			12_000,
			"switch"));

		Assert.Equal("EMatchState_Completed", completed.State);
		Assert.Equal(requester, Assert.Single(completed.Rounds).Winner);
		await using AsyncServiceScope verifyScope = provider.CreateAsyncScope();
		UserDataContext verify = verifyScope.ServiceProvider.GetRequiredService<UserDataContext>();
		MapStats score = await verify.MapScores.SingleAsync(value => value.ProfileId == requester);
		Assert.Equal("PokerFaceALT", score.MapName);
		Assert.Equal(12_000, score.HighScore);
	}

	[Fact]
	public async Task ConcurrentFinalizeRetryPersistsThePlayOnlyOnce()
	{
		await using ServiceProvider provider = await CreateProviderAsync();
		IChallengeMatchService service = provider.GetRequiredService<IChallengeMatchService>();
		Guid requester = Guid.NewGuid();
		Guid opponent = Guid.NewGuid();
		await using (AsyncServiceScope scope = provider.CreateAsyncScope())
		{
			UserDataContext context = scope.ServiceProvider.GetRequiredService<UserDataContext>();
			context.Profiles.Add(new Profile { Id = requester });
			context.MapScores.Add(new MapStats
			{
				ProfileId = opponent,
				MapName = "PokerFaceALT",
				HighScore = 10_000
			});
			await context.SaveChangesAsync();
		}

		Guid ugcId = Assert.IsType<Guid>(await service.FindChallengeForMapAsync(requester, "PokerFaceALT"));
		ChallengeMatchInfo created = Assert.IsType<ChallengeMatchInfo>(await service.CreateAsync(
			requester,
			ChallengeMatchType.Friendly,
			opponent,
			ugcId));
		Assert.NotNull(service.StartRound(created.MatchId, requester));

		ChallengeMatchInfo?[] results = await Task.WhenAll(
			service.FinalizeRoundAsync(created.MatchId, requester, 12_000, "switch").AsTask(),
			service.FinalizeRoundAsync(created.MatchId, requester, 12_000, "switch").AsTask());

		Assert.Contains(results, value => value?.State == "EMatchState_Completed");
		await using AsyncServiceScope verifyScope = provider.CreateAsyncScope();
		UserDataContext verify = verifyScope.ServiceProvider.GetRequiredService<UserDataContext>();
		MapStats score = await verify.MapScores.SingleAsync(value => value.ProfileId == requester);
		Assert.Equal(1, score.PlayCount);
	}

	private async Task<ServiceProvider> CreateProviderAsync()
	{
		await connection.OpenAsync();
		ServiceCollection services = new();
		services.AddDbContext<UserDataContext>(options => options.UseSqlite(connection));
		services.AddScoped<IScoreService>(provider => new ScoreService(
			provider.GetRequiredService<UserDataContext>(),
			NullLogger<ScoreService>.Instance));
		services.AddSingleton<TimeProvider>(TimeProvider.System);
		services.AddSingleton(Moq.Mock.Of<IMapService>());
		services.AddScoped<IPlayerActivityService, PlayerActivityService>();
		services.AddSingleton<IChallengeMatchService, ChallengeMatchService>();
		ServiceProvider provider = services.BuildServiceProvider();
		await using AsyncServiceScope scope = provider.CreateAsyncScope();
		await scope.ServiceProvider.GetRequiredService<UserDataContext>().Database.EnsureCreatedAsync();
		return provider;
	}

	public async ValueTask DisposeAsync()
	{
		await connection.DisposeAsync();
	}
}
