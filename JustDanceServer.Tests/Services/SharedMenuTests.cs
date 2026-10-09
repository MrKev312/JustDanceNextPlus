using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Options;

using Moq;

using System.Text;
using System.Text.Json;

namespace JustDanceServer.Tests.Services;

public sealed class SharedMenuTests
{
	[Fact]
	public async Task OneDefinitionSelectsTheSameMapsOnBothFamiliesWithoutMutatingTheBaseline()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		LocalizedStringService text = await SharedLocalizationTests.CreateAsync(SharedLocalizationTests.AuthoredText);
		MenuRowDefinition row = new() { Key = "favorites", TitleKey = "test.title", Policy = RecommendationPolicy.Favorites };
		MenuDefinitionService definitions = await LoadAsync([new() { Key = "test", Surfaces = ["jdnext-activity", "jdu-playlists"], Rows = [row] }], test, text);
		Mock<IPlaylistService> playlists = EmptyPlaylists();
		MenuService menus = new(definitions, test.Recommendations, test.Maps.Object, playlists.Object, text, test.Clock);
		await test.Preferences.SetFavoriteAsync(test.Player, "Alpha", true);
		await test.Preferences.SetFavoriteAsync(test.OtherPlayer, "Beta", true);
		IReadOnlyList<MenuRow> first = await menus.GetRowsAsync("jdnext-activity", test.Request() with { Locale = "fr-CA" });
		IReadOnlyList<MenuRow> second = await menus.GetRowsAsync("jdu-playlists", test.Request());
		IReadOnlyList<MenuRow> other = await menus.GetRowsAsync("jdnext-activity", test.Request() with { ProfileId = test.OtherPlayer });
		Assert.Equal(first[0].Id, second[0].Id);
		Assert.Equal(first[0].MapNames, second[0].MapNames);
		Assert.Equal("Titre canadien", first[0].Title);
		Assert.Equal(["Beta"], other[0].MapNames);
		ActivityPageResponse baseline = new();
		ActivityPageResponse projected = MenuProjection.AddRows(baseline, first, test.Maps.Object);
		Assert.Empty(baseline.Categories);
		CarouselCategory category = Assert.IsType<CarouselCategory>(Assert.Single(projected.Categories).Value);
		Assert.NotNull(category.ItemList);
		Assert.Equal(StableContentId.Create("Alpha"), Assert.IsType<MapTag>(Assert.Single(category.ItemList)).Guid);
		JduPlaylistDbResponse db = MenuProjection.AddPlaylists(new(), second, null);
		Assert.Equal(first[0].MapNames, db.Db["favorites"].Maps);
		Assert.Equal("recommended", db.Db["favorites"].Type);
	}

	[Theory]
	[InlineData("map")]
	[InlineData("playlist")]
	[InlineData("text")]
	[InlineData("duplicate")]
	[InlineData("schedule")]
	[InlineData("selector")]
	public async Task InvalidDefinitionsFailBeforeServingAnyRows(string invalid)
	{
		await using DiscoveryTestContext test = new();
		LocalizedStringService text = await SharedLocalizationTests.CreateAsync();
		MenuRowDefinition row = new() { Key = "row", TitleKey = "discovery.for-you" };
		row = invalid switch
		{
			"map" => row with { Source = MenuRowSource.Maps, Maps = ["Missing"] },
			"playlist" => row with { Source = MenuRowSource.Playlist, PlaylistId = Guid.NewGuid() },
			"text" => row with { TitleKey = "missing" },
			"schedule" => row with { StartsAt = test.Clock.Now, EndsAt = test.Clock.Now.AddDays(-1) },
			"selector" => row with { Source = (MenuRowSource)999 },
			_ => row
		};
		MenuDefinition definition = new() { Key = "test", Surfaces = ["jdnext-activity"], Rows = invalid == "duplicate" ? [row, row] : [row] };
		if (invalid == "text")
			await Assert.ThrowsAsync<KeyNotFoundException>(() => LoadAsync([definition], test, text));
		else
			await Assert.ThrowsAsync<InvalidOperationException>(() => LoadAsync([definition], test, text));
	}

	[Fact]
	public async Task ScheduleIsUtcAndClassicActionsSurviveProjection()
	{
		await using DiscoveryTestContext test = new();
		LocalizedStringService text = await SharedLocalizationTests.CreateAsync();
		MenuRowDefinition row = new()
		{
			Key = "scheduled", TitleKey = "discovery.for-you", Source = MenuRowSource.Maps, Maps = ["Alpha", "Beta"],
			StartsAt = test.Clock.Now.AddMinutes(1), EndsAt = test.Clock.Now.AddMinutes(2), Count = 1
		};
		MenuDefinitionService definitions = await LoadAsync([new() { Key = "test", Surfaces = ["jdu-classic-party"], Rows = [row] }], test, text);
		MenuService service = new(definitions, test.Recommendations, test.Maps.Object, EmptyPlaylists().Object, text, test.Clock);
		Assert.Empty(await service.GetRowsAsync("jdu-classic-party", test.Request(2017)));
		test.Clock.Now = test.Clock.Now.AddMinutes(1);
		IReadOnlyList<MenuRow> rows = await service.GetRowsAsync("jdu-classic-party", test.Request(2017));
		Assert.Equal(["Alpha"], Assert.Single(rows).MapNames);
		JduSongCarouselContentResponse baseline = new()
		{
			Categories = [new() { Title = "Existing favorites control" }],
			ActionLists = new() { ["favorite-control"] = new() }
		};
		JduSongCarouselContentResponse response = MenuProjection.AddRows(baseline, rows);
		Assert.Equal(2, response.Categories.Length);
		Assert.Single(baseline.Categories);
		Assert.True(response.ActionLists.ContainsKey("favorite-control"));
		Assert.Equal("partyMap", response.Categories[1].Items[0].ActionList);
		Assert.True(response.ActionLists.ContainsKey("partyMap"));
		test.Clock.Now = test.Clock.Now.AddMinutes(1);
		Assert.Empty(await service.GetRowsAsync("jdu-classic-party", test.Request(2017)));
	}

	[Fact]
	public async Task HomeRespectsCountDeliveryHistoryAndStableIdsWithoutTreatingDeliveryAsRead()
	{
		await using DiscoveryTestContext test = new();
		Guid newsId = Guid.NewGuid();
		Mock<IEditorialService> editorial = new();
		editorial.Setup(value => value.GetEntries(It.IsAny<EditorialAudience>())).Returns(
			[new EditorialEntry { Id = newsId, Title = "News", Body = "Body" }]);
		ModernJduMenuService service = new(test.Maps.Object, EmptyPlaylists().Object, editorial.Object, test.Clock);
		EditorialAudience audience = new(EditorialProduct.JDUnlimited, 2022, "switch");
		MenuRow[] rows = [new(Guid.NewGuid(), "for-you", "For You", null, ["Beta", "Gamma"], string.Empty)];
		JduHomeDataResponse first = service.CreateHomeData(audience, new() { RequestedTileCount = 2 }, rows);
		Assert.Equal(2, first.TileList.Length);
		JduHomeDataResponse unseen = service.CreateHomeData(audience, new() { RequestedTileCount = 1, TileHistory = first.TileList }, rows);
		Assert.Equal("Gamma", Assert.Single(unseen.TileList).MapTileInfo!.MapName);
		JduHomeDataResponse all = service.CreateHomeData(audience, new() { TileHistory = first.TileList, WatchedVideos = [Guid.NewGuid()] }, rows);
		Assert.False(all.TileList.Single(value => value.Uuid == newsId).New);
		Assert.Equal("News", all.TileList.Single(value => value.Uuid == newsId).NewsTileInfo!.Title);
		Assert.Equal(first.TileList.Single(value => value.Type == 0).Uuid, all.TileList.Single(value => value.MapTileInfo?.MapName == "Beta").Uuid);
		JduHomeDataResponse empty = service.CreateHomeData(audience, new() { RequestedTileCount = 0, TimestampLastMapContent = 42 }, rows);
		Assert.Empty(empty.TileList);
		Assert.Equal(42, empty.TimestampLastMapContent);
		Assert.Equal(0, empty.TimestampLastVideoContent);
	}

	[Fact]
	public void DynamicPlaylistMembershipAndCardsHaveTheSameStableKey()
	{
		MenuRow[] rows = [new(StableContentId.Create("menu:reco-for_you"), "reco-for_you", "For You", null, ["Alpha", "Beta"], "cover")];
		JduPlaylistDbResponse baseline = new() { Db = new() { ["curated"] = new() { Maps = ["Gamma"] } } };
		JduPlaylistDbResponse db = MenuProjection.AddPlaylists(baseline, rows, 1);
		JduCarouselContentResponse cards = MenuProjection.AddPlaylists(new JduCarouselContentResponse(), rows);
		string id = cards.Categories[0].Items[0].Components[0].PlaylistID;
		Assert.Equal("reco-for_you", id);
		Assert.Equal(["Alpha"], db.Db[id].Maps);
		Assert.Equal(["Gamma"], db.Db["curated"].Maps);
		Assert.Single(baseline.Db);
	}

	private static Mock<IPlaylistService> EmptyPlaylists()
	{
		Mock<IPlaylistService> service = new();
		service.SetupGet(value => value.Playlists).Returns(new Dictionary<Guid, ServerPlaylist>());
		return service;
	}

	private static async Task<MenuDefinitionService> LoadAsync(MenuDefinition[] definitions, DiscoveryTestContext test, ILocalizedStringService text)
	{
		Mock<IFileSystem> files = new();
		files.Setup(value => value.FileExists(It.IsAny<string>())).Returns(true);
		files.Setup(value => value.OpenRead(It.IsAny<string>())).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(definitions))));
		MenuDefinitionService service = new(Options.Create(new PathSettings()), files.Object,
			new JsonSettingsService(Mock.Of<IServiceProvider>()), test.Maps.Object, EmptyPlaylists().Object, text,
			Mock.Of<IActivityPageService>(value => value.ActivityPage == new ActivityPageResponse()));
		await service.LoadData();
		return service;
	}
}
