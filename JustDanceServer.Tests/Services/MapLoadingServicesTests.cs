using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using System.Text;
using System.Text.Json;

namespace JustDanceServer.Tests.Services;

public sealed class MapLoadingServicesTests
{
    private readonly JsonSettingsService _jsonSettingsService;
    private readonly IOptions<UrlSettings> _mockUrlOptions;
    private readonly Mock<ILogger<VideoFileInspector>> _mockVideoLogger;
    private readonly Mock<ILogger<ServerMapLoader>> _mockMapLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IWebmExtractor> _mockWebmExtractor;
    private readonly VideoFileInspector _videoInspector;
    private readonly MapMediaLoader _mediaLoader;
    private readonly JdPlusAssetMetadataLoader _assetMetadataLoader;
    private readonly ServerMapLoader _mapLoader;
    private readonly MapPackageReader _mapPackageReader;
    private readonly string _baseMapsPath = "maps"; // Use a relative path for tests

    public MapLoadingServicesTests()
    {
        _mockUrlOptions = Options.Create(new UrlSettings { CDNUrl = "test.cdn.com" });
        _mockVideoLogger = new Mock<ILogger<VideoFileInspector>>();
        _mockMapLogger = new Mock<ILogger<ServerMapLoader>>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockWebmExtractor = new Mock<IWebmExtractor>();

        // Mocks required by the JsonConverters
        Mock<ITagService> mockTagService = new();
        Mock<ILocalizedStringService> mockLocalizedStringService = new();
        Mock<IMapService> mockMapService = new();

        // This setup is required by custom JsonConverters which might be invoked
        // during the serialization/deserialization setup process.
        mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag(0)).Returns(new LocalizedString(0, ""));

        // Create concrete instances of the converters with mocked dependencies
        GuidTagConverter guidTagConverter = new(mockTagService.Object, mockLocalizedStringService.Object);
        MapTagConverter mapTagConverter = new(mockMapService.Object, Mock.Of<ILogger<MapTagConverter>>());
        TagIdConverter tagIdConverter = new(mockLocalizedStringService.Object);
        MapTagListConverter mapTagListConverter = new(mockMapService.Object, Mock.Of<ILogger<MapTagListConverter>>());

        // Mock the IServiceProvider to return the concrete converter instances
        Mock<IServiceProvider> mockServiceProvider = new();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(GuidTagConverter))).Returns(guidTagConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(MapTagConverter))).Returns(mapTagConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(TagIdConverter))).Returns(tagIdConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(MapTagListConverter))).Returns(mapTagListConverter);

        // Initialize the REAL JsonSettingsService with the correctly configured provider
        _jsonSettingsService = new JsonSettingsService(mockServiceProvider.Object);
        _jsonSettingsService.PrettyPascalFormat.Converters.Add(new ICategoryConverter());

        _videoInspector = new VideoFileInspector(
            _mockFileSystem.Object,
            _mockWebmExtractor.Object,
            _mockVideoLogger.Object);
        _mediaLoader = new MapMediaLoader(
            _mockUrlOptions,
            _mockFileSystem.Object,
            _videoInspector);
        _assetMetadataLoader = new JdPlusAssetMetadataLoader(_mockFileSystem.Object);
        _mapPackageReader = new MapPackageReader(_mockFileSystem.Object);
        _mapLoader = new ServerMapLoader(
            _jsonSettingsService,
            _mockMapLogger.Object,
            _mockFileSystem.Object,
            _mediaLoader,
            _mapPackageReader);
    }

    [Fact]
    public void GetVideoData_WhenNoVideosFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string videoFolder = "C:\\maps\\TestMap\\preview";
        _mockFileSystem.Setup(fs => fs.GetFiles(videoFolder, "*.webm", SearchOption.TopDirectoryOnly)).Returns([]);

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _videoInspector.GetVideoData(videoFolder));
    }

    [Fact]
    public void GetVideoData_WhenTooManyVideos_ThrowsInvalidOperationException()
    {
        // Arrange
        string videoFolder = "C:\\maps\\TestMap\\preview";
        _mockFileSystem.Setup(fs => fs.GetFiles(videoFolder, "*.webm", SearchOption.TopDirectoryOnly)).Returns(["1.webm", "2.webm", "3.webm", "4.webm", "5.webm"]);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _videoInspector.GetVideoData(videoFolder));
    }

    [Fact]
    public void GetVideoData_WhenCuesAreAtEnd_LogsWarning()
    {
        // Arrange
        string videoFolder = "C:\\maps\\TestMap\\preview";
        string videoFile = Path.Combine(videoFolder, "preview.webm");
        _mockFileSystem.Setup(fs => fs.GetFiles(videoFolder, "*.webm", SearchOption.TopDirectoryOnly)).Returns([videoFile]);
        _mockFileSystem.Setup(fs => fs.GetFileLength(videoFile)).Returns(10_000);
        _mockWebmExtractor.Setup(x => x.GetCuesInfo(videoFile)).Returns(new WebmData
        {
            FileName = "preview",
            Start = 9_500,
            End = 10_000,
            Duration = 30,
            Bitrate = 100_000
        });

        // Act
        WebmData[] result = _videoInspector.GetVideoData(videoFolder);

        // Assert
        Assert.Equal(4, result.Length);
        _mockVideoLogger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("has cues at the end")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void NormalizeSongInfoForBoot_WhenSweatDifficultyIsZero_DefaultsToOne()
    {
        // Arrange
        LocalJustDanceSongDBEntry songInfo = new()
        {
            MapName = "ExampleMap",
            SweatDifficulty = 0,
            DanceVersionLocId = new LocalizedString(0, "")
        };

        // Act
        LocalJustDanceSongDBEntry normalized = ServerMapLoader.NormalizeSongInfoForBoot(songInfo, "maps\\ExampleMap", _mockMapLogger.Object);

        // Assert
        Assert.Equal(1, normalized.SweatDifficulty);
        _mockMapLogger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("sweatDifficulty 0")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CuesAppearAtEnd_DetectsEndLoadedCues()
    {
        Assert.True(VideoFileInspector.CuesAppearAtEnd(new WebmData { Start = 9_500, End = 10_000 }, 10_000));
        Assert.False(VideoFileInspector.CuesAppearAtEnd(new WebmData { Start = 777, End = 834 }, 2_450_457));
    }

    [Fact]
    public void LoadAssetMetadata_CorrectlyBuildsMetadataDictionary()
    {
        // Arrange
        // Start with a relative path, just as the service would receive it.
        var relativeMapFolder = Path.Combine("maps", "TestMap");
        // Immediately convert it to a full path to mirror the service's internal logic.
        var mapFolder = Path.GetFullPath(relativeMapFolder);

		// Use the FULL path to build the dictionary keys for the mock setup.
		Dictionary<string, long> filesWithLengths = new()
		{
			[Path.Combine(mapFolder, "JDPlus", "Assets", "NX", "Cover", "abc.bundle")] = 100L,
			[Path.Combine(mapFolder, "JDPlus", "Preview", "def.opus")] = 200L,
			[Path.Combine(mapFolder, "JDPlus", "Audio", "ghi.opus")] = 300L,
            [Path.Combine(mapFolder, "JDPlus", "Assets", "NX", "CoachesSmall", "jkl.bundle")] = 400L,
            [Path.Combine(mapFolder, "JDPlus", "Assets", "NX", "CoachesLarge", "mno.bundle")] = 500L,
            [Path.Combine(mapFolder, "JDPlus", "Assets", "NX", "MapPackage", "pqr.bundle")] = 600L,
			[Path.Combine(mapFolder, "JDUnlimited", "Preview", "vp8", "pv8_small.webm")] = 800L,
			[Path.Combine(mapFolder, "JDUnlimited", "Preview", "vp8", "pv8_large.webm")] = 1200L,
			[Path.Combine(mapFolder, "JDUnlimited", "Preview", "vp9", "pv9_small.webm")] = 900L,
			[Path.Combine(mapFolder, "JDUnlimited", "Preview", "vp9", "pv9_large.webm")] = 1300L,
			[Path.Combine(mapFolder, "Video", "vp8", "small_video_vp8.webm")] = 1100L,
			[Path.Combine(mapFolder, "Video", "vp8", "medium_video_vp8.webm")] = 3100L,
			[Path.Combine(mapFolder, "Video", "vp8", "large_video_vp8.webm")] = 5100L,
			[Path.Combine(mapFolder, "Video", "vp8", "huge_video_vp8.webm")] = 10100L,
            [Path.Combine(mapFolder, "Video", "vp9", "small_video.webm")] = 1000L, //1st small video
            [Path.Combine(mapFolder, "Video", "vp9", "medium_video.webm")] = 3000L, //2nd medium video
            [Path.Combine(mapFolder, "Video", "vp9", "large_video.webm")] = 5000L, //3rd large video
            [Path.Combine(mapFolder, "Video", "vp9", "huge_video.webm")] = 10000L //4th huge video

        };

        // The mock must be set up to expect a call with the FULL path.
        _mockFileSystem.Setup(fs => fs.GetFiles(mapFolder, "*", SearchOption.AllDirectories))
        .Returns([.. filesWithLengths.Keys]);

        // For each fake file, set up a mock return for its fake length
        foreach (KeyValuePair<string, long> file in filesWithLengths)
        {
            _mockFileSystem.Setup(fs => fs.GetFileLength(file.Key)).Returns(file.Value);
        }

        // Act
        // Call the service with the original RELATIVE path.
		IReadOnlyDictionary<string, AssetMetadata> metadata = _assetMetadataLoader.Load(
			relativeMapFolder,
			ClientPlatform.NintendoSwitch,
			MapMediaLayouts.JDUnlimited);

        // Assert
        Assert.Equal(11, metadata.Count); // base assets +2 videoPreview +2 HIGH +1 ULTRA
        Assert.True(metadata.ContainsKey("cover"));
        Assert.Equal("abc", metadata["cover"].Hash);
        Assert.Equal(100L, metadata["cover"].Size);

        Assert.True(metadata.ContainsKey("mapPackage"));
        Assert.Equal("pqr", metadata["mapPackage"].Hash);
        Assert.Equal(600L, metadata["mapPackage"].Size);

        // videoPreview MID (second smallest by size)
        Assert.True(metadata.ContainsKey("videoPreview_MID.vp8.webm"));
        Assert.True(metadata.ContainsKey("videoPreview_MID.vp9.webm"));
        Assert.Equal("pv8_large", metadata["videoPreview_MID.vp8.webm"].Hash);
        Assert.Equal(1200L, metadata["videoPreview_MID.vp8.webm"].Size);
        Assert.Equal("pv9_large", metadata["videoPreview_MID.vp9.webm"].Hash);
        Assert.Equal(1300L, metadata["videoPreview_MID.vp9.webm"].Size);

        // Assert that the sorting logic correctly picked the largest file for HIGH
        Assert.True(metadata.ContainsKey("video_HIGH.hd.webm"));
		Assert.Equal("large_video_vp8", metadata["video_HIGH.hd.webm"].Hash);
		Assert.Equal(5100L, metadata["video_HIGH.hd.webm"].Size);
		Assert.Equal("large_video", metadata["video_HIGH.vp9.webm"].Hash);
		Assert.Equal(5000L, metadata["video_HIGH.vp9.webm"].Size);

        // Assert that the sorting logic correctly picked the largest file for ULTRA
        Assert.True(metadata.ContainsKey("video_ULTRA.hd.webm"));
		Assert.Equal("huge_video_vp8", metadata["video_ULTRA.hd.webm"].Hash);
		Assert.Equal(10100L, metadata["video_ULTRA.hd.webm"].Size);
    }

    [Fact]
    public async Task LoadMapAsync_MissingSongInfo_ThrowsException()
    {
        // Arrange
        string mapFolder = "C:\\maps\\TestMap";
        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(mapFolder, "SongInfo.json"))).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _mapLoader.LoadMapAsync(mapFolder).AsTask());
    }

    [Fact]
    public async Task LoadMapAsync_EmptyMapNameInSongInfo_ThrowsException()
    {
        // Arrange
        string mapFolder = "C:\\maps\\TestMap";
		LocalJustDanceSongDBEntry songInfo = new() { MapName = "", DanceVersionLocId = new LocalizedString(0, "") };
		string json = JsonSerializer.Serialize(songInfo, _jsonSettingsService.PrettyPascalFormat);
        MemoryStream stream = new(Encoding.UTF8.GetBytes(json));

        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(mapFolder, "SongInfo.json"))).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(Path.Combine(mapFolder, "SongInfo.json"))).Returns(stream);

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _mapLoader.LoadMapAsync(mapFolder).AsTask());
        Assert.NotNull(ex.InnerException);
        Assert.Contains("MapName is null or empty", ex.InnerException.Message);
    }

    [Fact]
    public void ReadMapPackageMetadata_MapPackageNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string mapPackagePath = "C:\\maps\\TestMap\\MapPackage\\file.bundle";
        _mockFileSystem.Setup(fs => fs.FileExists(mapPackagePath)).Returns(false);

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _mapPackageReader.ReadMetadata(mapPackagePath));
    }

    [Fact]
    public void GetVideoUrls_AssignsUrlsToCorrectOrder()
    {
        // Arrange
        string mapName = "TestMap";
        string mapFolder = Path.Combine(_baseMapsPath, mapName);
        WebmData[] videoData =
        [
            new() { FileName = "low_quality" },
            new() { FileName = "mid_quality" },
            new() { FileName = "high_quality" },
            new() { FileName = "ultra_quality" }
        ];
        string expectedBaseUrl = $"https://test.cdn.com/maps/{mapName}/JDPlus/Preview/vp9/";

        // Act
		IReadOnlyList<string> urls = _mediaLoader.GetVideoUrls(
			videoData,
			mapFolder,
			"JDPlus/Preview/vp9");

		// Assert
		Assert.Equal(
		[
			$"{expectedBaseUrl}low_quality.webm",
			$"{expectedBaseUrl}mid_quality.webm",
			$"{expectedBaseUrl}high_quality.webm",
			$"{expectedBaseUrl}ultra_quality.webm"
		], urls);
	}

	[Fact]
	public void LoadPlatformAssets_DiscoversPlatformsAndRolesForAnyMediaLayout()
	{
		string mapName = "TestMap";
		string mapFolder = Path.Combine(_baseMapsPath, mapName);
		string assetsFolder = Path.Combine(mapFolder, "JDUnlimited", "Assets");
		string[] platforms = ["NX", "WIIU"];
		string[] roles = ["Cover", "AlbumCoach", "Coach1"];
		_mockFileSystem.Setup(fs => fs.DirectoryExists(assetsFolder)).Returns(true);
		_mockFileSystem.Setup(fs => fs.GetDirectories(assetsFolder))
			.Returns([.. platforms.Select(platform => Path.Combine(assetsFolder, platform))]);

		foreach (string platform in platforms)
		{
			string platformFolder = Path.Combine(assetsFolder, platform);
			_mockFileSystem.Setup(fs => fs.GetDirectories(platformFolder))
				.Returns([.. roles.Select(role => Path.Combine(platformFolder, role))]);
			foreach (string role in roles)
			{
				string folder = Path.Combine(platformFolder, role);
				string file = Path.Combine(folder, role.ToLowerInvariant() + ".ckd");
				_mockFileSystem.Setup(fs => fs.GetFiles(folder, "*", SearchOption.TopDirectoryOnly)).Returns([file]);
			}
		}

		IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> assetsByPlatform =
			_mediaLoader.LoadPlatformAssets(mapFolder, MapMediaLayouts.JDUnlimited);

		Assert.Equal(platforms.Length, assetsByPlatform.Count);
		foreach (string platform in platforms)
		{
			IReadOnlyDictionary<string, string> assets = assetsByPlatform[platform];
			Assert.Equal(roles.Length, assets.Count);
			foreach (string role in roles)
			{
				Assert.Equal(
					$"https://test.cdn.com/maps/{mapName}/JDUnlimited/Assets/{platform}/{role}/{role.ToLowerInvariant()}.ckd",
					assets[role]);
			}
		}
	}

	[Theory]
	[InlineData(0)]
	[InlineData(2)]
	public void LoadPlatformAssets_RejectsNonSingularRoleFolders(int fileCount)
	{
		string mapFolder = Path.Combine(_baseMapsPath, "TestMap");
		string assetsFolder = Path.Combine(mapFolder, "JDPlus", "Assets");
		string platformFolder = Path.Combine(assetsFolder, "NX");
		string roleFolder = Path.Combine(platformFolder, "Cover");
		_mockFileSystem.Setup(fs => fs.DirectoryExists(assetsFolder)).Returns(true);
		_mockFileSystem.Setup(fs => fs.GetDirectories(assetsFolder)).Returns([platformFolder]);
		_mockFileSystem.Setup(fs => fs.GetDirectories(platformFolder)).Returns([roleFolder]);
		_mockFileSystem.Setup(fs => fs.GetFiles(roleFolder, "*", SearchOption.TopDirectoryOnly))
			.Returns([.. Enumerable.Range(0, fileCount).Select(index => Path.Combine(roleFolder, $"{index}.bundle"))]);

		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			_mediaLoader.LoadPlatformAssets(mapFolder, MapMediaLayouts.JDPlus));

		Assert.Contains("must contain exactly one file", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains($"found {fileCount}", exception.Message, StringComparison.OrdinalIgnoreCase);
	}
}
