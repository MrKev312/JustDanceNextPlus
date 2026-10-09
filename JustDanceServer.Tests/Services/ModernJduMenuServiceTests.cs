using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.AspNetCore.Mvc;

using Moq;

using ModernJduPlaylistsController = JustDanceServer.Controllers.prod.just_dance.com.carousel.v2.pages.ModernJduPlaylists;

namespace JustDanceServer.Tests.Services;

public sealed class ModernJduMenuServiceTests
{
    [Fact]
    public void MenuResponses_LinkPlaceholderContentToSharedCatalog()
    {
        OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
        songs[Guid.NewGuid()] = new JustDanceSongDBEntry
        {
            DanceVersionLocId = new OasisTag(new LocalizedString(1, "Test")),
            MapName = "Abracadabra",
            Title = "The Title"
        };
        songs[Guid.NewGuid()] = new JustDanceSongDBEntry
        {
            DanceVersionLocId = new OasisTag(new LocalizedString(1, "Excluded")),
            MapName = "ExcludedMap",
            Title = "Excluded"
        };

        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns(songs.ToServerMaps());
		Guid playlistId = Guid.NewGuid();
		Mock<IPlaylistService> playlistService = CreatePlaylistService(
			playlistId,
			["Abracadabra", "ExcludedMap"]);
		ModernJduMenuService service = new(
			mapService.Object,
			playlistService.Object,
			CreateEditorialService());

		JduHomeDataResponse home = service.CreateHomeData(new EditorialAudience(EditorialProduct.JDUnlimited, 2022, "switch"));
		JduPlaylistDbResponse playlistDatabase = service.CreatePlaylistDatabase();
		JduCarouselContentResponse carousel = service.CreatePlaylistCarousel();
		JduAnthologyCarouselContentResponse anthology = service.CreateAnthologyCarousel();

        Assert.Equal(ModernJduMenuService.PlaceholderText, home.TileList[0].NewsTileInfo?.Title);
        Assert.Equal(ModernJduMenuService.PlaceholderText, home.TileList[0].NewsTileInfo?.Text);
        Assert.Contains(home.TileList, tile => tile.MapTileInfo?.MapName == "Abracadabra");
		string playlistKey = playlistId.ToString("D");
        Assert.Contains(home.TileList, tile => tile.PlaylistTileInfo?.PlaylistID == playlistKey);

		JduPlaylist playlist = playlistDatabase.Db[playlistKey];
        Assert.Equal(["Abracadabra", "ExcludedMap"], playlist.Maps);
		Assert.Equal("Shared title", playlist.Title);
		Assert.Equal("Shared description", playlist.Description);

        Assert.Equal(
			playlistKey,
            carousel.Categories[0].Items[0].Components[0].PlaylistID);
        Assert.Equal("Abracadabra", anthology.Categories[0].Items[0].Components[0].MapName);
        Assert.Equal("ExcludedMap", anthology.Categories[0].Items[1].Components[0].MapName);
    }

    [Fact]
    public async Task PlaylistCarousel_UsesOneValidatedModernJduRoute()
    {
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps)
            .Returns([]);
		Mock<IPlaylistService> playlistService = CreatePlaylistService(Guid.NewGuid(), ["One", "Two"]);
		Mock<IMenuService> sharedMenus = new();
		sharedMenus.Setup(value => value.GetRowsAsync(It.IsAny<string>(), It.IsAny<PlayerRequestContext>()))
			.ReturnsAsync(Array.Empty<MenuRow>());
        ModernJduPlaylistsController controller = new(
			new ModernJduMenuService(mapService.Object, playlistService.Object, CreateEditorialService()),
			sharedMenus.Object, Mock.Of<ISessionManager>())
		{ ControllerContext = new() { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } };

        OkObjectResult[] playlists = await Task.WhenAll(Enumerable.Range(2019, 4)
            .Select(async year => Assert.IsType<OkObjectResult>(await controller.GetPlaylists(year))));
        OkObjectResult anthology = Assert.IsType<OkObjectResult>(controller.GetAnthology());

        Assert.All(playlists, result =>
            Assert.IsType<JustDanceServer.JustDanceClasses.Endpoints.JduCarouselContentResponse>(result.Value));
        Assert.IsType<NotFoundResult>(await controller.GetPlaylists(2018));
        Assert.IsType<JustDanceServer.JustDanceClasses.Endpoints.JduAnthologyCarouselContentResponse>(anthology.Value);
    }

    [Fact]
    public void AnthologyCarousel_DoesNotCapTheSharedCatalog()
    {
        OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
        for (int index = 0; index < 12; index++)
        {
            songs[Guid.NewGuid()] = new JustDanceSongDBEntry
            {
                DanceVersionLocId = new OasisTag(new LocalizedString(1, "Test")),
                MapName = $"Map{index:D2}",
                Title = $"Title {index:D2}"
            };
        }

        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns(songs.ToServerMaps());

		Mock<IPlaylistService> playlistService = CreatePlaylistService(Guid.NewGuid(), ["Map00", "Map01"]);
		JduAnthologyCarouselContentResponse anthology = new ModernJduMenuService(
			mapService.Object,
			playlistService.Object,
			CreateEditorialService()).CreateAnthologyCarousel();

        Assert.Equal(12, anthology.Categories[0].Items.Length);
    }

	[Fact]
	public void PlaylistDatabase_HonorsRequestedMapCount()
	{
		Mock<IMapService> mapService = new();
		mapService.SetupGet(service => service.Maps)
			.Returns([]);
		Guid playlistId = Guid.NewGuid();
		Mock<IPlaylistService> playlistService = CreatePlaylistService(
			playlistId,
			["One", "Two", "Three"]);

		JduPlaylistDbResponse response = new ModernJduMenuService(
			mapService.Object,
			playlistService.Object,
			CreateEditorialService())
			.CreatePlaylistDatabase(2);

		Assert.Equal(["One", "Two"], response.Db[playlistId.ToString("D")].Maps);
	}

	private static Mock<IPlaylistService> CreatePlaylistService(Guid id, string[] mapNames)
	{
		Mock<IPlaylistService> service = new();
		service.SetupGet(value => value.Playlists).Returns(
			new Dictionary<Guid, ServerPlaylist>
			{
				[id] = new ServerPlaylist
				{
					Id = id,
					Name = "Shared playlist",
					Title = "Shared title",
					Description = "Shared description",
					MapNames = mapNames
				}
			});
		return service;
	}

	private static IEditorialService CreateEditorialService()
	{
		Mock<IEditorialService> service = new();
		service.Setup(value => value.GetEntries(It.IsAny<EditorialAudience>()))
			.Returns(
			[
				new EditorialEntry
				{
					Id = Guid.NewGuid(),
					Title = ModernJduMenuService.PlaceholderText,
					Body = ModernJduMenuService.PlaceholderText
				}
			]);
		return service.Object;
	}
}
