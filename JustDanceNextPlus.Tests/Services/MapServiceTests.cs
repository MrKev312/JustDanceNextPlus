using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace JustDanceNextPlus.Tests.Services;

public class MapServiceTests
{
    private readonly Mock<IOptions<PathSettings>> _mockPathSettings;
    private readonly Mock<IOptions<UrlSettings>> _mockUrlSettings;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<MapService>> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IUtilityService> _mockUtilityService;
    private readonly Mock<ITagService> _mockTagService;
    private readonly Mock<IBundleService> _mockBundleService;
    private readonly MapService _service;

    public MapServiceTests()
    {
        _mockPathSettings = new Mock<IOptions<PathSettings>>();
        _mockUrlSettings = new Mock<IOptions<UrlSettings>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<MapService>>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockUtilityService = new Mock<IUtilityService>();
        _mockTagService = new Mock<ITagService>();
        _mockBundleService = new Mock<IBundleService>();

        _mockPathSettings.Setup(o => o.Value).Returns(new PathSettings { MapsPath = "C:\\maps" });
        _mockUrlSettings.Setup(o => o.Value).Returns(new UrlSettings { HostUrl = "http://localhost" });

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IUtilityService))).Returns(_mockUtilityService.Object);
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
    public async Task LoadOffers_AssignsUntaggedSongsToJdPlus()
    {
		// Arrange
		Guid songId = Guid.NewGuid();
		string mapName = "untaggedMap";
		string mapFolder = $"C:\\maps\\{mapName}";
        _mockFileSystem.Setup(fs => fs.GetDirectories(It.IsAny<string>())).Returns([mapName]);
        _mockUtilityService.Setup(u => u.LoadMapDBEntryAsync(mapFolder))
            .ReturnsAsync(new LocalJustDanceSongDBEntry { SongID = songId, MapName = mapName, Tags = [], Artist = "Artist", TagIds = [], DanceVersionLocId = new LocalizedString(0, "") });

		ProductGroup jdPlusProductGroup = new()
		{ Type = "jdplus",
            GroupLocId = new LocalizedString(1, "JD Plus"),
            SongsCountLocId = new LocalizedString(2, "Count"),
            GroupDescriptionLocId = new LocalizedString(3, "Description"),
            TracklistExtendedLocId = new LocalizedString(4, "Extended"),
            TracklistLimitedLocId = new LocalizedString(5, "Limited")
            };
		Guid jdPlusGuid = Guid.NewGuid();

		ShopConfig shopConfig = new();
        shopConfig.FirstPartyProductDb.ProductGroups.Add(jdPlusGuid, jdPlusProductGroup);

        // FIX: Set up the mock to return the real data object from the top-level property.
        _mockBundleService.SetupGet(bs => bs.ShopConfig).Returns(shopConfig);

        // Act
        await _service.LoadData(); // This will run LoadMapsAsync first, then LoadOffers

		// Assert
		Dictionary<string, Pack> claims = _service.SongDBTypeSet.SongOffers.Claims;
        Assert.True(claims.ContainsKey("jdplus"));
        Assert.Contains(songId, claims["jdplus"].SongIds);
        Assert.Contains("jdplus", _service.Songs[songId].Tags);
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
    public async Task LoadMapsAsync_PopulatesAllDictionariesCorrectly()
    {
		// Arrange
		Guid map1Id = Guid.NewGuid();
		Guid map2Id = Guid.NewGuid();
		string map1Folder = Path.Combine("C:\\maps", "map1");
		string map2Folder = Path.Combine("C:\\maps", "map2");

		// Data for the first map
		LocalJustDanceSongDBEntry songInfo1 = new() { SongID = map1Id, MapName = "map1", Artist = "Artist A", CoachCount = 1, TagIds = [], DanceVersionLocId = new LocalizedString(0, "") };
		ContentAuthorization auth1 = new();
		Dictionary<string, AssetMetadata> metadata1 = new() { ["cover"] = new("hash1", 123) };

		// Data for the second map
		LocalJustDanceSongDBEntry songInfo2 = new() { SongID = map2Id, MapName = "map2", Artist = "Artist B", CoachCount = 2, TagIds = [], DanceVersionLocId = new LocalizedString(0, "") };
		ContentAuthorization auth2 = new();
		Dictionary<string, AssetMetadata> metadata2 = new() { ["cover"] = new("hash2", 456) };

        _mockFileSystem.Setup(fs => fs.GetDirectories("C:\\maps")).Returns([map1Folder, map2Folder]);

        _mockUtilityService.Setup(u => u.LoadMapDBEntryAsync(map1Folder)).ReturnsAsync(songInfo1);
        _mockUtilityService.Setup(u => u.LoadContentAuthorization(map1Folder)).Returns(auth1);
        _mockUtilityService.Setup(u => u.LoadAssetMetadata(map1Folder)).Returns(metadata1);

        _mockUtilityService.Setup(u => u.LoadMapDBEntryAsync(map2Folder)).ReturnsAsync(songInfo2);
        _mockUtilityService.Setup(u => u.LoadContentAuthorization(map2Folder)).Returns(auth2);
        _mockUtilityService.Setup(u => u.LoadAssetMetadata(map2Folder)).Returns(metadata2);

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
            GroupLocId = new OasisTag(new LocalizedString(1, "JD+")),
            SongsCountLocId = new OasisTag(new LocalizedString(2, "Count")),
            GroupDescriptionLocId = new OasisTag(new LocalizedString(3, "Description")),
            TracklistExtendedLocId = new OasisTag(new LocalizedString(4, "Extended")),
            TracklistLimitedLocId = new OasisTag(new LocalizedString(5, "Limited"))
        });
        _mockBundleService.SetupGet(bs => bs.ShopConfig).Returns(shopConfig);
        _mockBundleService.Setup(bs => bs.LoadData()).Returns(Task.CompletedTask);

        // Act
        await _service.LoadData(); // This calls LoadMapsAsync internally

        // Assert
        Assert.Equal(2, _service.Songs.Count);
        Assert.True(_service.Songs.ContainsKey(map1Id));
        Assert.Equal("map1", _service.Songs[map1Id].MapName);

        Assert.Equal(2, _service.ContentAuthorization.Count);
        Assert.True(_service.ContentAuthorization.ContainsKey(map2Id));
        Assert.Equal(auth2, _service.ContentAuthorization[map2Id]);

        Assert.Equal(2, _service.AssetMetadataPerSong.Count);
        Assert.True(_service.AssetMetadataPerSong.ContainsKey(map1Id));
        Assert.Equal(metadata1, _service.AssetMetadataPerSong[map1Id]);

        Assert.Equal(2, _service.MapToGuid.Count);
        Assert.Equal(map2Id, _service.MapToGuid["map2"]);

        // Verify that the tag service was called for artists and coach counts
        _mockTagService.Verify(ts => ts.GetAddTag("Artist A", "artist"), Times.Once);
        _mockTagService.Verify(ts => ts.GetAddTag("Solo", "choreoSettings"), Times.Once);
        _mockTagService.Verify(ts => ts.GetAddTag("Duet", "choreoSettings"), Times.Once);
    }
}