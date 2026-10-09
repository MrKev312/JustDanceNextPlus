using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Tests.Services;

public sealed class SharedRecommendationTests
{
	[Fact]
	public async Task FavoritesAreIdempotentAndPrivateToCanonicalPlayer()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		Assert.True(await test.Preferences.SetFavoriteAsync(test.Player, "Alpha", true));
		DateTime addedAt = (await test.Db.PlayerMapFavorites.AsNoTracking().SingleAsync()).FavoritedAtUtc;
		test.Clock.Now = test.Clock.Now.AddDays(1);
		Assert.True(await test.Preferences.SetFavoriteAsync(test.Player, "Alpha", true));
		Assert.Equal(addedAt, (await test.Db.PlayerMapFavorites.AsNoTracking().SingleAsync()).FavoritedAtUtc);
		Assert.Equal(["Alpha"], await test.Preferences.GetFavoritesAsync(test.Player));
		Assert.Empty(await test.Preferences.GetFavoritesAsync(test.OtherPlayer));
		Assert.False(await test.Preferences.SetFavoriteAsync(Guid.NewGuid(), "Alpha", true));
		Assert.False(await test.Preferences.SetFavoriteAsync(test.Player, "Unknown", true));
		Assert.True(await test.Preferences.SetFavoriteAsync(test.Player, "Alpha", false));
		Assert.True(await test.Preferences.SetFavoriteAsync(test.Player, "Alpha", false));
		Assert.Empty(await test.Preferences.GetFavoritesAsync(test.Player));
	}

	[Fact]
	public async Task FavoriteAndRecentPoliciesUseTheSameSharedStateWithoutLeakingOtherPlayers()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		await test.Preferences.SetFavoriteAsync(test.Player, "Alpha", true);
		await test.Preferences.SetFavoriteAsync(test.OtherPlayer, "Beta", true);
		await test.Activity.RecordJdNextObservationAsync(test.Player, "Gamma", 11000, "switch", false);
		test.Clock.Now = test.Clock.Now.AddMinutes(1);
		await test.Activity.RecordJdNextObservationAsync(test.Player, "Delta", 10000, "switch", false);
		Assert.Equal(["Alpha"], await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.Favorites, test.Player)));
		Assert.Equal(["Beta"], await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.Favorites, test.OtherPlayer)));
		Assert.Equal(["Delta", "Gamma"], await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.Recent, test.Player)));
		Assert.Empty(await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.Recent, test.OtherPlayer)));
		Assert.Empty(await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.Favorites)));
	}

	[Fact]
	public async Task DiscoveryIsStableWithinUtcDayAndPrioritizesUnknownSongs()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		await test.Activity.StoreOrdinalsAsync(test.Player, "legacy", new Dictionary<string, long> { ["Alpha"] = 300 });
		await test.Activity.RecordJdNextObservationAsync(test.Player, "Beta", 10000, "switch", false);
		RecommendationQuery query = new(RecommendationPolicy.Discover, test.Player, 4);
		string[] first = await test.Recommendations.GetMapsAsync(query);
		test.Clock.Now = test.Clock.Now.AddHours(4);
		Assert.Equal(first, await test.Recommendations.GetMapsAsync(query));
		Assert.DoesNotContain("Alpha", first);
		Assert.DoesNotContain("Beta", first);
		Assert.Equal(4, first.Distinct().Count());
		Assert.Empty(await test.Recommendations.GetMapsAsync(query with { Count = 0 }));
		Assert.Equal(6, (await test.Recommendations.GetMapsAsync(query with { Count = 1000 })).Length);
		Assert.Empty(await test.Db.MapScores.ToListAsync());
	}

	[Fact]
	public async Task ReplayAndCountryRankingsHaveDeterministicGlobalFallbacks()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		test.Db.MapScores.AddRange(
			new MapStats { ProfileId = test.Player, MapName = "Gamma", PlayCount = 4 },
			new MapStats { ProfileId = test.OtherPlayer, MapName = "Beta", PlayCount = 20 });
		test.Db.PlayerMetadata.AddRange(new PlayerMetadata { ProfileId = test.Player, CountryId = 1 },
			new PlayerMetadata { ProfileId = test.OtherPlayer, CountryId = 2 });
		await test.Db.SaveChangesAsync();
		Assert.Equal("Gamma", (await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.TopReplayed, test.Player)))[0]);
		Assert.Equal("Gamma", (await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.TopCountry, test.Player)))[0]);
		Assert.Equal("Beta", (await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.TopCountry)))[0]);
		Assert.Equal("Beta", (await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.Popular)))[0]);
		test.Db.MapScores.RemoveRange(await test.Db.MapScores.Where(value => value.ProfileId == test.Player).ToArrayAsync());
		await test.Db.SaveChangesAsync();
		Assert.Equal("Beta", (await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.TopCountry, test.Player)))[0]);
	}

	[Fact]
	public async Task ForYouUsesFavoritesAsRelatedSignalsAndKeepsDailyOrderStable()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		OrderedDictionary<Guid, ServerMap> catalog = test.Maps.Object.Maps;
		Guid beta = StableContentId.Create("Beta");
		catalog[beta] = catalog[beta] with { Song = catalog[beta].Song with { Artist = "Alpha" } };
		await test.Preferences.SetFavoriteAsync(test.Player, "Alpha", true);
		RecommendationQuery query = new(RecommendationPolicy.ForYou, test.Player, 4);
		string[] first = await test.Recommendations.GetMapsAsync(query);
		Assert.Equal("Beta", first[0]);
		test.Clock.Now = test.Clock.Now.AddHours(1);
		Assert.Equal(first, await test.Recommendations.GetMapsAsync(query));
	}

	[Fact]
	public async Task PersonalRowsDeduplicateParentVariantsButJduSortingRetainsTheFullCatalog()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
		foreach (string name in new[] { "Original", "Alternate", "Other" })
			songs.Add(Guid.NewGuid(), new JustDanceSongDBEntry
			{
				MapName = name, Title = name, ParentMapName = name == "Alternate" ? "Original" : string.Empty,
				DanceVersionLocId = new LocalizedString(0, string.Empty)
			});
		test.Maps.SetupGet(value => value.Maps).Returns(songs.ToServerMaps());
		string[] personal = await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.ForYou, test.Player));
		Assert.Equal(2, personal.Length);
		Assert.False(personal.Contains("Original") && personal.Contains("Alternate"));
		JduRecommendationService adapter = new(test.Recommendations);
		string[] full = await adapter.GetOrderedMapsAsync(test.Player);
		Assert.Equal(3, full.Length);
		Assert.Equal(personal, full.Take(personal.Length));
	}
}
