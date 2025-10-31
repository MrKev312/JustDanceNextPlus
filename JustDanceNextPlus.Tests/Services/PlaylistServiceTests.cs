using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using System.Text;

namespace JustDanceNextPlus.Tests.Services;

public class PlaylistServiceTests
{
    private readonly Mock<IMapService> _mockMapService;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILogger<PlaylistService>> _mockLogger;
    private readonly JsonSettingsService _jsonSettingsService;
    private readonly IOptions<PathSettings> _mockPathSettings;
    private readonly PlaylistService _service;
    private readonly Mock<ITagService> _mockTagService;
    private readonly Mock<ILocalizedStringService> _mockLocalizedStringService;

    public PlaylistServiceTests()
    {
        _mockMapService = new Mock<IMapService>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockLogger = new Mock<ILogger<PlaylistService>>();
        _mockPathSettings = Options.Create(new PathSettings { PlaylistPath = "C:\\playlists" });

        // Mocks required by the JsonConverters
        _mockTagService = new Mock<ITagService>();
        _mockLocalizedStringService = new Mock<ILocalizedStringService>();

        // Initialize the mock to handle the GetLocalizedTag(0) call
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag(0)).Returns(new LocalizedString(0, ""));

		// Create concrete instances of the converters with mocked dependencies
		GuidTagConverter guidTagConverter = new(_mockTagService.Object, _mockLocalizedStringService.Object);
		MapTagConverter mapTagConverter = new(_mockMapService.Object, Mock.Of<ILogger<MapTagConverter>>());
		TagIdConverter tagIdConverter = new(_mockLocalizedStringService.Object);
		MapTagListConverter mapTagListConverter = new(_mockMapService.Object, Mock.Of<ILogger<MapTagListConverter>>());

		// Mock the IServiceProvider to return the concrete converter instances
		Mock<IServiceProvider> mockServiceProvider = new();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(GuidTagConverter))).Returns(guidTagConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(MapTagConverter))).Returns(mapTagConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(TagIdConverter))).Returns(tagIdConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(MapTagListConverter))).Returns(mapTagListConverter);

        // Initialize the service with the correctly configured provider
        _jsonSettingsService = new JsonSettingsService(mockServiceProvider.Object);

        _service = new PlaylistService(
            _mockMapService.Object,
            _jsonSettingsService,
            _mockPathSettings,
            _mockLogger.Object,
            _mockFileSystem.Object
        );
    }

    [Fact]
    public async Task LoadData_AppliesQueryAndOrderByCorrectly()
    {
		// Arrange
		Guid map1Id = Guid.NewGuid();
		Guid map2Id = Guid.NewGuid();
		Guid map3Id = Guid.NewGuid();
		OrderedDictionary<Guid, JustDanceSongDBEntry> songDb = new()
		{
            [map1Id] = new() { MapName = "Map1", DanceVersionLocId = new LocalizedString(0, ""), OriginalJDVersion = 2022 },
            [map2Id] = new() { MapName = "Map2", DanceVersionLocId = new LocalizedString(0, ""), OriginalJDVersion = 2024 },
            [map3Id] = new() { MapName = "Map3", DanceVersionLocId = new LocalizedString(0, ""), OriginalJDVersion = 2023 }
        };
		Dictionary<string, Guid> mapToGuid = new()
		{
            ["Map1"] = map1Id,
            ["Map2"] = map2Id,
            ["Map3"] = map3Id
        };
        _mockMapService.Setup(m => m.Songs).Returns(songDb);
        _mockMapService.Setup(m => m.MapToGuid).Returns(mapToGuid);

		Guid playlistGuid = Guid.NewGuid();
        string playlistJson = $$"""
        {
          "localizedTitle": 0,
          "localizedDescription": 0,
          "guid": "{{playlistGuid}}",
          "query": "OriginalJDVersion > 2022",
          "orderBy": "OriginalJDVersion descending"
        }
        """;

        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.json", SearchOption.TopDirectoryOnly)).Returns(["playlist.json"]);
        _mockFileSystem.Setup(fs => fs.OpenRead("playlist.json")).Returns(new MemoryStream(Encoding.UTF8.GetBytes(playlistJson)));

        // Act
        await _service.LoadData();

        // Assert
        Assert.True(_service.PlaylistDB.Playlists.ContainsKey(playlistGuid));
		JustDancePlaylist playlist = _service.PlaylistDB.Playlists[playlistGuid];
        Assert.Equal(2, playlist.ItemList.Count);
        Assert.Equal(map2Id, playlist.ItemList[0].Id); // 2024
        Assert.Equal(map3Id, playlist.ItemList[1].Id); // 2023
    }

    [Fact]
    public async Task LoadData_SkipsPlaylistWithNotEnoughMaps()
    {
		// Arrange
		Guid map1Id = Guid.NewGuid();
		OrderedDictionary<Guid, JustDanceSongDBEntry> songDb = new() { [map1Id] = new() { MapName = "Map1", DanceVersionLocId = new LocalizedString(0, "") } };
        _mockMapService.Setup(m => m.Songs).Returns(songDb);

		Guid playlistGuid = Guid.NewGuid();
        string playlistJson = $$"""
        {
          "localizedTitle": 0,
          "localizedDescription": 0,
          "guid": "{{playlistGuid}}",
          "itemList": ["{{map1Id}}"]
        }
        """; // Only one map

        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.json", SearchOption.TopDirectoryOnly)).Returns(["playlist.json"]);
        _mockFileSystem.Setup(fs => fs.OpenRead("playlist.json")).Returns(new MemoryStream(Encoding.UTF8.GetBytes(playlistJson)));

        // Act
        await _service.LoadData();

        // Assert
        Assert.False(_service.PlaylistDB.Playlists.ContainsKey(playlistGuid));
        _mockLogger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("has not enough maps, skipping")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}