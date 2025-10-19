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

public class BundleServiceTests
{
    private readonly Mock<ILogger<BundleService>> _mockLogger;
    private readonly IOptions<PathSettings> _mockPathSettings;
    private readonly IOptions<UrlSettings> _mockUrlSettings;
    private readonly Mock<ILocalizedStringService> _mockLocalizedStringService;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly JsonSettingsService _jsonSettingsService;
    private readonly BundleService _service;

    public BundleServiceTests()
    {
        _mockLogger = new Mock<ILogger<BundleService>>();
        _mockPathSettings = Options.Create(new PathSettings { JsonsPath = "C:\\jsons" });
        _mockUrlSettings = Options.Create(new UrlSettings { CDNUrl = "test.cdn.com" });
        _mockLocalizedStringService = new Mock<ILocalizedStringService>();
        _mockFileSystem = new Mock<IFileSystem>();

		// Mocks required by the JsonConverters
		Mock<ITagService> mockTagService = new();
		Mock<IMapService> mockMapService = new();

		// Create concrete instances of the converters with mocked dependencies
		GuidTagConverter guidTagConverter = new(mockTagService.Object, _mockLocalizedStringService.Object);
		MapTagConverter mapTagConverter = new(mockMapService.Object, Mock.Of<ILogger<MapTagConverter>>());
		TagIdConverter tagIdConverter = new(_mockLocalizedStringService.Object);
		MapTagListConverter mapTagListConverter = new(mockMapService.Object, Mock.Of<ILogger<MapTagListConverter>>());

		// Mock the IServiceProvider to return the concrete converter instances
		Mock<IServiceProvider> mockServiceProvider = new();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(GuidTagConverter))).Returns(guidTagConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(MapTagConverter))).Returns(mapTagConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(TagIdConverter))).Returns(tagIdConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(MapTagListConverter))).Returns(mapTagListConverter);

        _jsonSettingsService = new JsonSettingsService(mockServiceProvider.Object);

        _service = new BundleService(
            _mockLogger.Object,
            _mockPathSettings,
            _mockUrlSettings,
            _jsonSettingsService,
            _mockLocalizedStringService.Object,
            _mockFileSystem.Object
        );
    }

    [Fact]
    public async Task LoadLiveTiles_FileExists_LoadsAndReplacesCdnUrl()
    {
		// Arrange
		Guid tileId = Guid.NewGuid();
        // Added required properties buttonId and subtitleId to the JSON
        string fakeJson = $$$"""
        {
          "{{{tileId}}}": {
            "buttonId": 123,
            "subtitleId": 456,
            "assets": {
              "backgroundImage": "{{cdnUrl}}/path/to/image.png"
            }
          }
        }
        """;
		MemoryStream fakeStream = new(Encoding.UTF8.GetBytes(fakeJson));
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(fakeStream);

        // Setup the localized string service for the new IDs used in the JSON
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag(123)).Returns(new LocalizedString(123, "Button Text"));
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag(456)).Returns(new LocalizedString(456, "Subtitle Text"));

        // Act
        await _service.LoadLiveTiles();

        // Assert
        Assert.Single(_service.LiveTiles);
        Assert.True(_service.LiveTiles.ContainsKey(tileId));
        Assert.Equal("test.cdn.com/path/to/image.png", _service.LiveTiles[tileId].Assets.BackgroundImage);
        Assert.Equal(123, _service.LiveTiles[tileId].ButtonId.ID);
        Assert.Equal(456, _service.LiveTiles[tileId].SubtitleId.ID);
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Live tiles database loaded")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadJustDanceBundles_FileExists_ParsesAndInitializesClaims()
    {
		// Arrange
		JustDanceEdition fakeEdition = new()
		{
            Name = "songpack_year1",
            DlcType = VersionType.Yearly,
            ProductGroupBundle = "{{cdnUrl}}/bundle.bundle",
            GroupLocId = new LocalizedString(1234, "Song Pack 2023"),
        };
		List<JustDanceEdition> db = [fakeEdition];
		string fakeJson = JsonSerializer.Serialize(db, _jsonSettingsService.PrettyPascalFormat);
		MemoryStream fakeStream = new(Encoding.UTF8.GetBytes(fakeJson));

        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(_mockPathSettings.Value.JsonsPath, "JustDanceEditions.json"))).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(Path.Combine(_mockPathSettings.Value.JsonsPath, "JustDanceEditions.json"))).Returns(fakeStream);
        _mockLocalizedStringService.Setup(ls => ls.GetAddLocalizedTag(It.IsAny<string>())).Returns((string s) => new LocalizedString(s.GetHashCode(), s));
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag(It.IsAny<int>())).Returns((int i) => new LocalizedString(i, $"Loc_{i}"));

        // Act
        await _service.LoadData();

        // Assert
        Assert.NotNull(_service.ShopConfig);
        Assert.Equal(2, _service.ShopConfig.FirstPartyProductDb.ProductGroups.Count); // 1 edition + 1 JD+
        Assert.Single(_service.ShopConfig.FirstPartyProductDb.DlcProducts);

		DlcProduct product = _service.ShopConfig.FirstPartyProductDb.DlcProducts.Values.First();
        Assert.Equal("songpack_year1", product.Name);

		ProductGroup productGroup = _service.ShopConfig.FirstPartyProductDb.ProductGroups.Values.First(pg => pg.Name == "songpack_year1");
        Assert.Equal("test.cdn.com/bundle.bundle", productGroup.Assets.ProductGroupBundle);

        // We have one claim in total
        Assert.Single(_service.Claims);
        Assert.Contains("songpack_year1", _service.Claims);

        // We have 2 claims in the display priority list, jdplus is always added
        Assert.Equal(2, _service.ClaimDisplayPriority.Count);
        Assert.Equal("songpack_year1", _service.ClaimDisplayPriority[0]);
        Assert.Equal("jdplus", _service.ClaimDisplayPriority[1]);
    }

    [Fact]
    public async Task LoadData_HandlesMissingFilesGracefully()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        await _service.LoadData();

        // Assert
        Assert.NotNull(_service.ShopConfig);
        Assert.Empty(_service.LiveTiles);
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Live tiles database not found")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bundle database not found")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAllClaims_ReturnsLoadedClaimsAndJdPlus()
    {
        // Arrange
        // First, load some data to populate the internal claims list
        JustDanceEdition fakeEdition = new()
        {
            Name = "songpack_year1",
            DlcType = VersionType.Yearly,
            ProductGroupBundle = "{{cdnUrl}}/bundle.bundle",
            GroupLocId = new LocalizedString(1234, "Song Pack 2023"),
        };
        List<JustDanceEdition> db = [fakeEdition];
        string fakeJson = JsonSerializer.Serialize(db, _jsonSettingsService.PrettyPascalFormat);
        MemoryStream fakeStream = new(Encoding.UTF8.GetBytes(fakeJson));

        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(_mockPathSettings.Value.JsonsPath, "JustDanceEditions.json"))).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(Path.Combine(_mockPathSettings.Value.JsonsPath, "JustDanceEditions.json"))).Returns(fakeStream);
        _mockLocalizedStringService.Setup(ls => ls.GetAddLocalizedTag(It.IsAny<string>())).Returns((string s) => new LocalizedString(s.GetHashCode(), s));
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag(It.IsAny<int>())).Returns((int i) => new LocalizedString(i, $"Loc_{i}"));

        await _service.LoadData();

        // Act
        List<string> allClaims = _service.GetAllClaims();

        // Assert
        Assert.Single(allClaims);
        Assert.Contains("songpack_year1", allClaims);
    }
}