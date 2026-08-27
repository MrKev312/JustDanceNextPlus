using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace JustDanceServer.Tests.Services;

public class MapServiceTests
{
    private readonly Mock<IOptions<PathSettings>> _mockPathSettings;
    private readonly Mock<IOptions<UrlSettings>> _mockUrlSettings;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<MapService>> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IServerMapLoader> _mockMapLoader;
    private readonly Mock<IJdPlusAssetMetadataLoader> _mockAssetMetadataLoader;
    private readonly Mock<ITagService> _mockTagService;
    private readonly Mock<IBundleService> _mockBundleService;
    private readonly MapService _service;
    private readonly string _baseMapsPath = "C:\\maps";

    public MapServiceTests()
    {
        _mockPathSettings = new Mock<IOptions<PathSettings>>();
        _mockUrlSettings = new Mock<IOptions<UrlSettings>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<MapService>>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockMapLoader = new Mock<IServerMapLoader>();
        _mockAssetMetadataLoader = new Mock<IJdPlusAssetMetadataLoader>();
        _mockTagService = new Mock<ITagService>();
        _mockBundleService = new Mock<IBundleService>();

        _mockPathSettings.Setup(o => o.Value).Returns(new PathSettings { MapsPath = _baseMapsPath });
        _mockUrlSettings.Setup(o => o.Value).Returns(new UrlSettings { HostUrl = "http://localhost" });

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServerMapLoader))).Returns(_mockMapLoader.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IJdPlusAssetMetadataLoader))).Returns(_mockAssetMetadataLoader.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ITagService))).Returns(_mockTagService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IBundleService))).Returns(_mockBundleService.Object);

        _service = new MapService(
            _mockPathSettings.Object,
            _mockUrlSettings.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockFileSystem.Object
        );
    }

    [Fact]
    public async Task LoadRecentlyAddedMaps_SelectsMapsWithinOneHourOfLatest()
    {
		// Arrange
		DateTime now = DateTime.UtcNow;
		string[] directories = ["mapA", "mapB", "mapC", "mapD", "mapE", "mapF"];
		Dictionary<string, DateTime> creationTimes = new()
		{
            ["mapA"] = now.AddHours(-10), // Not needed
            ["mapB"] = now.AddHours(-3),  // Too much difference
            ["mapC"] = now.AddHours(-1.1),// Needed for 4th map
            ["mapD"] = now.AddMinutes(-50),// Recent
            ["mapE"] = now.AddMinutes(-20),// Recent
            ["mapF"] = now.AddMinutes(-5), // Latest
        };
		Dictionary<string, Guid> mapIds = new()
		{
            ["mapA"] = Guid.NewGuid(),
            ["mapB"] = Guid.NewGuid(),
            ["mapC"] = Guid.NewGuid(),
            ["mapD"] = Guid.NewGuid(),
            ["mapE"] = Guid.NewGuid(),
            ["mapF"] = Guid.NewGuid(),
        };

        _mockFileSystem.Setup(fs => fs.GetDirectories(It.IsAny<string>())).Returns(directories);
        foreach (string? dir in directories)
        {
            _mockFileSystem.Setup(fs => fs.GetDirectoryCreationTime(dir)).Returns(creationTimes[dir]);
        }

        foreach ((string? name, Guid id) in mapIds)
        {
            _service.MapToGuid[name] = id;
        }

		// Act
		List<MapTag> recentlyAdded = await _service.LoadRecentlyAddedMaps();

        // Assert
        Assert.Equal(4, recentlyAdded.Count);
        Assert.Contains(recentlyAdded, m => m.Guid == mapIds["mapF"]);
        Assert.Contains(recentlyAdded, m => m.Guid == mapIds["mapE"]);
        Assert.Contains(recentlyAdded, m => m.Guid == mapIds["mapD"]);
        Assert.Contains(recentlyAdded, m => m.Guid == mapIds["mapC"]);
    }

    [Fact]
    public async Task LoadRecentlyAddedMaps_SelectsAtLeastFourIfTimesAreClose()
    {
		// Arrange
		DateTime now = DateTime.UtcNow;
		string[] directories = ["mapA", "mapB", "mapC", "mapD", "mapE"];
		Dictionary<string, DateTime> creationTimes = new()
		{
            ["mapA"] = now.AddMinutes(-40),
            ["mapB"] = now.AddMinutes(-30),
            ["mapC"] = now.AddMinutes(-20),
            ["mapD"] = now.AddMinutes(-10),
            ["mapE"] = now.AddMinutes(-5),
        };
		Dictionary<string, Guid> mapIds = directories.ToDictionary(d => d, d => Guid.NewGuid());
        _mockFileSystem.Setup(fs => fs.GetDirectories(It.IsAny<string>())).Returns(directories);
        foreach (string? dir in directories)
        {
            _mockFileSystem.Setup(fs => fs.GetDirectoryCreationTime(dir)).Returns(creationTimes[dir]);
            _service.MapToGuid[dir] = mapIds[dir];
        }

		// Act
		List<MapTag> recentlyAdded = await _service.LoadRecentlyAddedMaps();

        // Assert
        Assert.Equal(5, recentlyAdded.Count);
    }

    [Fact]
    public void GetSongId_WhenMapExists_ReturnsCorrectGuid()
    {
		// Arrange
		Guid expectedGuid = Guid.NewGuid();
		string mapName = "TestMap";
        _service.MapToGuid[mapName] = expectedGuid;

		// Act
		Guid? result = _service.GetSongId(mapName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public void GetSongId_WhenMapDoesNotExist_ReturnsNull()
    {
        // Arrange
        _service.MapToGuid["SomeOtherMap"] = Guid.NewGuid();

		// Act
		Guid? result = _service.GetSongId("NonExistentMap");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSongId_IsCaseSensitive_ReturnsNullForMismatchedCase()
    {
		// Arrange
		Guid expectedGuid = Guid.NewGuid();
		string mapName = "CaseSensitiveMap";
        _service.MapToGuid[mapName] = expectedGuid;

		// Act
		Guid? result = _service.GetSongId("casesensitivemap");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadData_LoadsEachFolderOnceIntoSingleMapCollection()
    {
		// Arrange
		Guid map1Id = Guid.NewGuid();
		Guid map2Id = Guid.NewGuid();
		string map1Folder = Path.Combine("C:\\maps", "map1");
		string map2Folder = Path.Combine("C:\\maps", "map2");

		// Data for the first map
		LocalJustDanceSongDBEntry songInfo1 = new() { SongID = map1Id, MapName = "map1", Artist = "Artist A", CoachCount = 1, TagIds = [], DanceVersionLocId = new LocalizedString(0, "") };
		Dictionary<string, AssetMetadata> metadata1 = new() { ["cover"] = new("hash1", 123) };

		// Data for the second map
		LocalJustDanceSongDBEntry songInfo2 = new() { SongID = map2Id, MapName = "map2", Artist = "Artist B", CoachCount = 2, TagIds = [], DanceVersionLocId = new LocalizedString(0, "") };
		Dictionary<string, AssetMetadata> metadata2 = new() { ["cover"] = new("hash2", 456) };
		IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> jdPlusPlatforms =
			new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
			{
				[ClientPlatform.NintendoSwitch] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					["Cover"] = "switch-cover",
					["CoachesLarge"] = "switch-coaches-large",
					["CoachesSmall"] = "switch-coaches-small",
					["MapPackage"] = "switch-map-package"
				},
				[ClientPlatform.PC] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					["Cover"] = "pc-cover",
					["CoachesLarge"] = "pc-coaches-large",
					["CoachesSmall"] = "pc-coaches-small",
					["MapPackage"] = "pc-map-package"
				}
			};
		MapMediaAssets jdPlusMedia = ServerMapTestData.CreateMedia(MapMediaLayouts.JDPlus, jdPlusPlatforms);

        _mockFileSystem.Setup(fs => fs.GetDirectories("C:\\maps")).Returns([map1Folder, map2Folder]);

		_mockMapLoader
			.Setup(u => u.LoadMapAsync(map1Folder))
			.ReturnsAsync(ServerMapTestData.CreateMap(map1Id, map1Folder, songInfo1, jdPlus: jdPlusMedia));
		_mockMapLoader
			.Setup(u => u.LoadMapAsync(map2Folder))
			.ReturnsAsync(ServerMapTestData.CreateMap(map2Id, map2Folder, songInfo2, jdPlus: jdPlusMedia));
        _mockAssetMetadataLoader
			.Setup(loader => loader.Load(
				map1Folder,
				ClientPlatform.NintendoSwitch,
				MapMediaLayouts.JDUnlimited))
            .Returns(metadata1);
        _mockAssetMetadataLoader
			.Setup(loader => loader.Load(
				map2Folder,
				ClientPlatform.NintendoSwitch,
				MapMediaLayouts.JDUnlimited))
            .Returns(metadata2);

        // Correctly mock TagService to return a Tag with a valid OasisTag
        _mockTagService.Setup(ts => ts.GetAddTag(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns((string text, string category) => new Tag
                       {
                           TagName = text,
                           Category = category,
                           LocId = new OasisTag(new LocalizedString(text.GetHashCode(), text))
                       });

		// Correctly initialize ShopConfig with a fully valid ProductGroup
		ShopConfig shopConfig = new();
        shopConfig.FirstPartyProductDb.ProductGroups.Add(Guid.NewGuid(), new ProductGroup
        {
            Type = "jdplus",
            GroupLocId = new LocalizedString(1, "JD+"),
            SongsCountLocId = new LocalizedString(2, "Count"),
            GroupDescriptionLocId = new LocalizedString(3, "Description"),
            TracklistExtendedLocId = new LocalizedString(4, "Extended"),
            TracklistLimitedLocId = new LocalizedString(5, "Limited")
        });
        _mockBundleService.SetupGet(bs => bs.ShopConfig).Returns(shopConfig);
        _mockBundleService.Setup(bs => bs.LoadData()).Returns(Task.CompletedTask);

        // Act
		await Task.WhenAll(_service.LoadData(), _service.LoadData());
		await _service.LoadData();

        // Assert
        Assert.Equal(2, _service.Maps.Count);
        Assert.True(_service.Maps.ContainsKey(map1Id));
        Assert.Equal("map1", _service.Maps[map1Id].Song.MapName);
        Assert.Equal(map1Folder, _service.Maps[map1Id].FolderPath);
		JustDanceSongDBEntry switchSong = _service.GetSongs(
			ClientPlatform.NintendoSwitch,
			MapMediaLayouts.JDUnlimited)[map1Id];
		Assert.Equal("switch-cover", switchSong.Assets.Cover);
		Assert.Contains("/JDUnlimited/Preview/vp8/", switchSong.Assets.VideoPreview_MID_vp8_webm);
		Assert.Contains("/JDUnlimited/Preview/vp9/", switchSong.Assets.VideoPreview_MID_vp9_webm);
		Assert.StartsWith("https://cdn.test/JDPlus/", switchSong.Assets.AudioPreview_opus);
		JustDanceSongDBEntry laterSwitchSong = _service.GetSongs(
			ClientPlatform.NintendoSwitch,
			MapMediaLayouts.JDPlus)[map1Id];
		Assert.Contains("/JDPlus/Preview/vp8/", laterSwitchSong.Assets.VideoPreview_MID_vp8_webm);
		Assert.Contains("/JDPlus/Preview/vp9/", laterSwitchSong.Assets.VideoPreview_MID_vp9_webm);
		Assert.Equal(
			"switch-map-package",
			_service.GetContentAuthorization(ClientPlatform.NintendoSwitch)[map2Id].Assets.MapPackage);
		ContentAuthorization pcAuthorization = _service.GetContentAuthorization(ClientPlatform.PC)[map1Id];
		Assert.Contains("/Video/vp8/high.webm", pcAuthorization.Assets.VideoHighHdWebm);
		Assert.Contains("/Video/vp9/high.webm", pcAuthorization.Assets.VideoHighVP9Webm);
		Assert.Contains("codecs=\"vp8\"", pcAuthorization.AssetsMetadata.VideoMpd);
		Assert.Contains("video_HIGH.hd.webm", pcAuthorization.AssetsMetadata.VideoMpd);

		ContentAuthorization switchAuthorization =
			_service.GetContentAuthorization(ClientPlatform.NintendoSwitch)[map1Id];
		Assert.Contains("codecs=\"vp9\"", switchAuthorization.AssetsMetadata.VideoMpd);
		Assert.Contains("video_HIGH.vp9.webm", switchAuthorization.AssetsMetadata.VideoMpd);
		Assert.Equal(
			metadata1,
			_service.GetAssetMetadataPerSong(
				ClientPlatform.NintendoSwitch,
				MapMediaLayouts.JDUnlimited)[map1Id]);

        Assert.Equal(2, _service.MapToGuid.Count);
        Assert.Equal(map2Id, _service.MapToGuid["map2"]);

        _mockMapLoader.Verify(
            utility => utility.LoadMapAsync(map1Folder),
            Times.Once);
        _mockMapLoader.Verify(
            utility => utility.LoadMapAsync(map2Folder),
            Times.Once);

        // Verify that the tag service was called for artists and coach counts
        _mockTagService.Verify(ts => ts.GetAddTag("Artist A", "artist"), Times.Once);
        _mockTagService.Verify(ts => ts.GetAddTag("Solo", "choreoSettings"), Times.Once);
        _mockTagService.Verify(ts => ts.GetAddTag("Duet", "choreoSettings"), Times.Once);
    }

    [Fact]
    public async Task LoadData_CatchesAndLogsException_WhenLoadFails()
    {
		// Arrange
		InvalidOperationException exception = new("Test exception");
        _mockFileSystem.Setup(fs => fs.GetDirectories(It.IsAny<string>())).Throws(exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LoadData());

        // Verify that the error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error loading data")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadMapsAsync_SplitsMultipleArtistsUsingRegex()
    {
        // Arrange
        Guid mapId = Guid.NewGuid();
        string mapFolder = Path.Combine(_baseMapsPath, "map1");
        LocalJustDanceSongDBEntry songInfo = new() { SongID = mapId, MapName = "map1", Artist = "Artist A & Artist B ft. Artist C", CoachCount = 1, TagIds = [], DanceVersionLocId = new LocalizedString(0, "") };

        _mockFileSystem.Setup(fs => fs.GetDirectories(_baseMapsPath)).Returns([mapFolder]);
        _mockMapLoader
            .Setup(u => u.LoadMapAsync(mapFolder))
			.ReturnsAsync(ServerMapTestData.CreateMap(mapId, mapFolder, songInfo));

        // Mocks for LoadOffers
        _mockBundleService.SetupGet(bs => bs.ShopConfig).Returns(new ShopConfig());
        _mockBundleService.Setup(bs => bs.LoadData()).Returns(Task.CompletedTask);

        // Act
        await _service.LoadData();

        // Assert
        _mockTagService.Verify(ts => ts.GetAddTag("Artist A", "artist"), Times.Once);
        _mockTagService.Verify(ts => ts.GetAddTag("Artist B", "artist"), Times.Once);
        _mockTagService.Verify(ts => ts.GetAddTag("Artist C", "artist"), Times.Once);
    }

    [Fact]
    public async Task LoadRecentlyAddedMaps_HandlesFewerThanFourMaps()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        string[] directories = ["mapA", "mapB"];
		Dictionary<string, DateTime> creationTimes = new()
		{
            ["mapA"] = now.AddMinutes(-20),
            ["mapB"] = now.AddMinutes(-10)
        };
		Dictionary<string, Guid> mapIds = directories.ToDictionary(d => d, d => Guid.NewGuid());
        _mockFileSystem.Setup(fs => fs.GetDirectories(It.IsAny<string>())).Returns(directories);
        foreach (var dir in directories)
        {
            _mockFileSystem.Setup(fs => fs.GetDirectoryCreationTime(dir)).Returns(creationTimes[dir]);
            _service.MapToGuid[dir] = mapIds[dir];
        }

		// Act
		List<MapTag> recentlyAdded = await _service.LoadRecentlyAddedMaps();

        // Assert
        Assert.Equal(2, recentlyAdded.Count);
        Assert.Contains(recentlyAdded, m => m.Guid == mapIds["mapA"]);
        Assert.Contains(recentlyAdded, m => m.Guid == mapIds["mapB"]);
    }

    [Fact]
    public async Task LoadRecentlyAddedMaps_IgnoresMapsNotInGuidDictionary()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        string[] directories = ["mapA", "mapB_no_guid", "mapC"];
		Dictionary<string, DateTime> creationTimes = new()
		{
            ["mapA"] = now.AddMinutes(-20),
            ["mapB_no_guid"] = now.AddMinutes(-15),
            ["mapC"] = now.AddMinutes(-10),
        };

        _mockFileSystem.Setup(fs => fs.GetDirectories(It.IsAny<string>())).Returns(directories);
        foreach (var dir in directories)
        {
            _mockFileSystem.Setup(fs => fs.GetDirectoryCreationTime(dir)).Returns(creationTimes[dir]);
        }

        // Only add mapA and mapC to the GUID dictionary
        _service.MapToGuid["mapA"] = Guid.NewGuid();
        _service.MapToGuid["mapC"] = Guid.NewGuid();

		// Act
		List<MapTag> recentlyAdded = await _service.LoadRecentlyAddedMaps();

        // Assert
        Assert.Equal(2, recentlyAdded.Count);
        Assert.Contains(recentlyAdded, m => m.Guid == _service.MapToGuid["mapA"]);
        Assert.Contains(recentlyAdded, m => m.Guid == _service.MapToGuid["mapC"]);
    }
}
