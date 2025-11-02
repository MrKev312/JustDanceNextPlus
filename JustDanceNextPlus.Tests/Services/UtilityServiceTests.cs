using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using System.Text;
using System.Text.Json;

namespace JustDanceNextPlus.Tests.Services;

public class UtilityServiceTests
{
    private readonly JsonSettingsService _jsonSettingsService;
    private readonly IOptions<UrlSettings> _mockUrlOptions;
    private readonly Mock<ILogger<UtilityService>> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IWebmExtractor> _mockWebmExtractor;
    private readonly UtilityService _service;
    private readonly string _baseMapsPath = "maps"; // Use a relative path for tests

    public UtilityServiceTests()
    {
        _mockUrlOptions = Options.Create(new UrlSettings { CDNUrl = "test.cdn.com" });
        _mockLogger = new Mock<ILogger<UtilityService>>();
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

        _service = new UtilityService(
        _jsonSettingsService,
        _mockUrlOptions,
        _mockLogger.Object,
        _mockFileSystem.Object,
        _mockWebmExtractor.Object
        );
    }

    [Fact]
    public void GetAssetUrl_WhenFileExists_ReturnsCorrectUrl()
    {
        // Arrange
        string mapName = "TestMap";
        string mapFolder = Path.Combine(_baseMapsPath, mapName);
        string assetName = "cover";
        string assetFolder = Path.Combine(mapFolder, assetName);
        string expectedFile = Path.Combine(assetFolder, "cover.png");
        _mockFileSystem.Setup(fs => fs.DirectoryExists(assetFolder)).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(assetFolder, "*", SearchOption.TopDirectoryOnly)).Returns([expectedFile]);

        // Act
        string result = _service.GetAssetUrl(mapFolder, assetName);

        // Assert
        Assert.Equal($"https://test.cdn.com/maps/{mapName}/cover/cover.png", result);
    }

    [Fact]
    public void GetAssetUrl_WhenDirectoryMissing_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        string mapFolder = "C:\\maps\\TestMap";
        string assetName = "cover";
        string assetFolder = Path.Combine(mapFolder, assetName);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(assetFolder)).Returns(false);

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _service.GetAssetUrl(mapFolder, assetName));
    }

    [Fact]
    public void TryGetAssetUrl_WhenCanBeMissing_ReturnsNullOnMissingDirectory()
    {
        // Arrange
        string mapFolder = "C:\\maps\\TestMap";
        string assetName = "songTitleLogo";
        string assetFolder = Path.Combine(mapFolder, assetName);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(assetFolder)).Returns(false);

        // Act
        string? result = _service.TryGetAssetUrl(mapFolder, assetName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetVideoData_WhenNoVideosFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string videoFolder = "C:\\maps\\TestMap\\videoPreview";
        _mockFileSystem.Setup(fs => fs.GetFiles(videoFolder, "*.webm", SearchOption.TopDirectoryOnly)).Returns([]);

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _service.GetVideoData(videoFolder));
    }

    [Fact]
    public void GetVideoData_WhenTooManyVideos_ThrowsInvalidOperationException()
    {
        // Arrange
        string videoFolder = "C:\\maps\\TestMap\\videoPreview";
        _mockFileSystem.Setup(fs => fs.GetFiles(videoFolder, "*.webm", SearchOption.TopDirectoryOnly)).Returns(["1.webm", "2.webm", "3.webm", "4.webm", "5.webm"]);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _service.GetVideoData(videoFolder));
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
            [Path.Combine(mapFolder, "cover", "abc.png")] = 100L,
            [Path.Combine(mapFolder, "audioPreview_opus", "def.opus")] = 200L,
            [Path.Combine(mapFolder, "audio", "ghi.opus")] = 300L,
            [Path.Combine(mapFolder, "coachesSmall", "jkl.png")] = 400L,
            [Path.Combine(mapFolder, "coachesLarge", "mno.png")] = 500L,
            [Path.Combine(mapFolder, "MapPackage", "pqr.bundle")] = 600L,
            [Path.Combine(mapFolder, "videoPreview", "pv_small.webm")] = 800L,
            [Path.Combine(mapFolder, "videoPreview", "pv_large.webm")] = 1200L,
            [Path.Combine(mapFolder, "video", "small_video.webm")] = 1000L, //1st small video
            [Path.Combine(mapFolder, "video", "medium_video.webm")] = 3000L, //2nd medium video
            [Path.Combine(mapFolder, "video", "large_video.webm")] = 5000L, //3rd large video
            [Path.Combine(mapFolder, "video", "huge_video.webm")] = 10000L //4th huge video

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
        Dictionary<string, AssetMetadata> metadata = _service.LoadAssetMetadata(relativeMapFolder);

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
        string expectedPreviewHash = "pv_large"; // second smallest out of two is the larger one
        Assert.Equal(expectedPreviewHash, metadata["videoPreview_MID.vp8.webm"].Hash);
        Assert.Equal(1200L, metadata["videoPreview_MID.vp8.webm"].Size);

        // Assert that the sorting logic correctly picked the largest file for HIGH
        Assert.True(metadata.ContainsKey("video_HIGH.hd.webm"));
        Assert.Equal("large_video", metadata["video_HIGH.hd.webm"].Hash);
        Assert.Equal(5000L, metadata["video_HIGH.hd.webm"].Size);

        // Assert that the sorting logic correctly picked the largest file for ULTRA
        Assert.True(metadata.ContainsKey("video_ULTRA.hd.webm"));
        Assert.Equal("huge_video", metadata["video_ULTRA.hd.webm"].Hash);
        Assert.Equal(10000L, metadata["video_ULTRA.hd.webm"].Size);
    }

    [Fact]
    public async Task LoadMapDBEntryAsync_MissingSongInfo_ThrowsException()
    {
        // Arrange
        string mapFolder = "C:\\maps\\TestMap";
        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(mapFolder, "SongInfo.json"))).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LoadMapDBEntryAsync(mapFolder).AsTask());
    }

    [Fact]
    public async Task LoadMapDBEntryAsync_EmptyMapNameInSongInfo_ThrowsException()
    {
        // Arrange
        string mapFolder = "C:\\maps\\TestMap";
		LocalJustDanceSongDBEntry songInfo = new() { MapName = "", DanceVersionLocId = new LocalizedString(0, "") };
		string json = JsonSerializer.Serialize(songInfo, _jsonSettingsService.PrettyPascalFormat);
        MemoryStream stream = new(Encoding.UTF8.GetBytes(json));

        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(mapFolder, "SongInfo.json"))).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(Path.Combine(mapFolder, "SongInfo.json"))).Returns(stream);

        string videoPreviewFolder = Path.Combine(mapFolder, "videoPreview");
        _mockFileSystem.Setup(fs => fs.GetFiles(videoPreviewFolder, "*.webm", SearchOption.TopDirectoryOnly)).Returns(["C:\\fake\\video.webm"]);

        _mockWebmExtractor.Setup(x => x.GetCuesInfo(It.IsAny<string>())).Returns(new WebmData());

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LoadMapDBEntryAsync(mapFolder).AsTask());
        Assert.NotNull(ex.InnerException);
        Assert.Contains("MapName is null or empty", ex.InnerException.Message);
    }

    [Fact]
    public void GenerateAudioPreviewTrk_MapPackageNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string mapPackagePath = "C:\\maps\\TestMap\\MapPackage\\file.bundle";
        _mockFileSystem.Setup(fs => fs.FileExists(mapPackagePath)).Returns(false);

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _service.GenerateAudioPreviewTrk(mapPackagePath));
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
        string expectedBaseUrl = $"https://test.cdn.com/maps/{mapName}/videoPreview/";

        // Act
        (string low, string mid, string high, string ultra) = _service.GetVideoUrls(videoData, mapFolder);

        // Assert
        Assert.Equal($"{expectedBaseUrl}low_quality.webm", low);
        Assert.Equal($"{expectedBaseUrl}mid_quality.webm", mid);
        Assert.Equal($"{expectedBaseUrl}high_quality.webm", high);
        Assert.Equal($"{expectedBaseUrl}ultra_quality.webm", ultra);
    }

    [Fact]
    public void GetAssetUrls_ReturnsUrls()
    {
        // Arrange
        string mapName = "TestMap";
        string mapFolder = Path.Combine(_baseMapsPath, mapName);

        // audioPreview_opus
        string audioPreviewFolder = Path.Combine(mapFolder, "audioPreview_opus");
        _mockFileSystem.Setup(fs => fs.DirectoryExists(audioPreviewFolder)).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(audioPreviewFolder, "*", SearchOption.TopDirectoryOnly)).Returns([Path.Combine(audioPreviewFolder, "preview.opus")]);

        // coachesLarge
        string coachesLargeFolder = Path.Combine(mapFolder, "coachesLarge");
        _mockFileSystem.Setup(fs => fs.DirectoryExists(coachesLargeFolder)).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(coachesLargeFolder, "*", SearchOption.TopDirectoryOnly)).Returns([Path.Combine(coachesLargeFolder, "cl.png")]);

        // coachesSmall
        string coachesSmallFolder = Path.Combine(mapFolder, "coachesSmall");
        _mockFileSystem.Setup(fs => fs.DirectoryExists(coachesSmallFolder)).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(coachesSmallFolder, "*", SearchOption.TopDirectoryOnly)).Returns([Path.Combine(coachesSmallFolder, "cs.png")]);

        // cover
        string coverFolder = Path.Combine(mapFolder, "cover");
        _mockFileSystem.Setup(fs => fs.DirectoryExists(coverFolder)).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(coverFolder, "*", SearchOption.TopDirectoryOnly)).Returns([Path.Combine(coverFolder, "cover.png")]);

        // songTitleLogo missing
        string songTitleLogoFolder = Path.Combine(mapFolder, "songTitleLogo");
        _mockFileSystem.Setup(fs => fs.DirectoryExists(songTitleLogoFolder)).Returns(false);

        // Act
        (string audioPreview, string coachesLarge, string coachesSmall, string cover, string? songTitleLogo) = _service.GetAssetUrls(mapFolder);

        // Assert
        Assert.Equal($"https://test.cdn.com/maps/{mapName}/audioPreview_opus/preview.opus", audioPreview);
        Assert.Equal($"https://test.cdn.com/maps/{mapName}/coachesLarge/cl.png", coachesLarge);
        Assert.Equal($"https://test.cdn.com/maps/{mapName}/coachesSmall/cs.png", coachesSmall);
        Assert.Equal($"https://test.cdn.com/maps/{mapName}/cover/cover.png", cover);
        Assert.Null(songTitleLogo);
    }

    [Fact]
    public void GetContentAuthorizationVideoUrls_AssignsUrlsToCorrectProperties()
    {
        // Arrange
        string mapName = "TestMap";
        string mapFolder = Path.Combine(_baseMapsPath, mapName);
        WebmData[] videoData =
        [
            new() { FileName = "low_video" },
            new() { FileName = "mid_video" },
            new() { FileName = "high_video" },
            new() { FileName = "ultra_video" }
        ];
        string expectedBaseUrl = $"https://test.cdn.com/maps/{mapName}/video/";

        // Act
        (string low, string mid, string high, string ultra) = _service.GetContentAuthorizationVideoUrls(videoData, mapFolder);

        // Assert
        Assert.Equal($"{expectedBaseUrl}low_video.webm", low);
        Assert.Equal($"{expectedBaseUrl}mid_video.webm", mid);
        Assert.Equal($"{expectedBaseUrl}high_video.webm", high);
        Assert.Equal($"{expectedBaseUrl}ultra_video.webm", ultra);
    }
}