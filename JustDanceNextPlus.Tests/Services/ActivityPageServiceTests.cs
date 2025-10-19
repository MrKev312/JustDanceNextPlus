using Moq;
using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace JustDanceNextPlus.Tests.Services;

public class ActivityPageServiceTests
{
    private readonly Mock<ILogger<ActivityPageService>> _mockLogger;
    private readonly IOptions<PathSettings> _mockPathSettings;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IMapService> _mockMapService;
    private readonly Mock<ITagService> _mockTagService;
    private readonly Mock<ILocalizedStringService> _mockLocalizedStringService;
    private readonly JsonSettingsService _jsonSettingsService;
    private readonly ActivityPageService _service;

    public ActivityPageServiceTests()
    {
        _mockLogger = new Mock<ILogger<ActivityPageService>>();
        _mockPathSettings = Options.Create(new PathSettings { JsonsPath = "C:\\fake" });
        _mockFileSystem = new Mock<IFileSystem>();
        _mockMapService = new Mock<IMapService>();
        _mockTagService = new Mock<ITagService>();
        _mockLocalizedStringService = new Mock<ILocalizedStringService>();

        // Setup mock for JsonSettingsService and its dependencies
        _jsonSettingsService = CreateJsonSettingsService(_mockMapService, _mockTagService, _mockLocalizedStringService);

        _service = new ActivityPageService(
            _mockLogger.Object,
            _mockPathSettings,
            _jsonSettingsService,
            _mockMapService.Object,
            _mockLocalizedStringService.Object,
            _mockFileSystem.Object
        );
    }

    private static JsonSettingsService CreateJsonSettingsService(
        Mock<IMapService> mockMapService,
        Mock<ITagService> mockTagService,
        Mock<ILocalizedStringService> mockLocalizedStringService)
    {
		GuidTagConverter guidTagConverter = new(mockTagService.Object, mockLocalizedStringService.Object);
		MapTagConverter mapTagConverter = new(mockMapService.Object, Mock.Of<ILogger<MapTagConverter>>());
		TagIdConverter tagIdConverter = new(mockLocalizedStringService.Object);
		MapTagListConverter mapTagListConverter = new(mockMapService.Object, Mock.Of<ILogger<MapTagListConverter>>());

		Mock<IServiceProvider> mockServiceProvider = new();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(GuidTagConverter))).Returns(guidTagConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(MapTagConverter))).Returns(mapTagConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(TagIdConverter))).Returns(tagIdConverter);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(MapTagListConverter))).Returns(mapTagListConverter);

        return new JsonSettingsService(mockServiceProvider.Object);
    }

    [Fact]
    public async Task LoadData_WhenFileExists_LoadsDataCorrectly()
    {
        // Arrange
        string existingCategoryId = "a3e9c0a3-3a5e-4e6d-8b2b-0e1f7b8a7d6c";
        string fakeJson = $$"""
        {
          "categories": {
            "{{existingCategoryId}}": {
              "categoryName": "Existing Test Category",
              "type": "carousel",
              "titleId": 1001,
              "itemList": []
            }
          },
          "categoryModifiers": [
            {
              "name": "positionModifier",
              "itemTypeToModify": "carousel",
              "position": {
                "id": "{{existingCategoryId}}",
                "position": 1
              }
            }
          ]
        }
        """;
		MemoryStream fakeStream = new(Encoding.UTF8.GetBytes(fakeJson));

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(fakeStream);

        _mockMapService.Setup(ms => ms.RecentlyAdded).Returns([]);

		LocalizedString existingLocalizedString = new(1001, "Existing Test Category");
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag(1001)).Returns(existingLocalizedString);

		LocalizedString newLocalizedString = new(12345, "Newly Added Songs!");
        _mockLocalizedStringService.Setup(ls => ls.GetAddLocalizedTag("Newly Added Songs!")).Returns(newLocalizedString);

        // Act
        await _service.LoadData();

        // Assert
        Assert.NotNull(_service.ActivityPage);
        Assert.Single(_service.ActivityPage.Categories);

		CarouselCategory? newCategory = _service.ActivityPage.Categories.Values.OfType<CarouselCategory>().FirstOrDefault(c => c.CategoryName == "Newly Added Songs");
        Assert.Null(newCategory);

        Assert.Single(_service.ActivityPage.CategoryModifiers);
		PositionModifier firstModifier = Assert.IsType<PositionModifier>(_service.ActivityPage.CategoryModifiers[0]);
        Assert.Equal(1, firstModifier.Position.Position);
    }

    [Fact]
    public async Task LoadData_WhenFileExists_LoadsAndAddsNewSongsCategory()
    {
        // Arrange
        string existingCategoryId = "a3e9c0a3-3a5e-4e6d-8b2b-0e1f7b8a7d6c";
        string fakeJson = $$"""
        {
          "categories": {
            "{{existingCategoryId}}": {
              "categoryName": "Existing Test Category",
              "type": "carousel",
              "titleId": 1001,
              "itemList": []
            }
          },
          "categoryModifiers": [
            {
              "name": "positionModifier",
              "itemTypeToModify": "carousel",
              "position": {
                "id": "{{existingCategoryId}}",
                "position": 1
              }
            }
          ]
        }
        """;
		MemoryStream fakeStream = new(Encoding.UTF8.GetBytes(fakeJson));

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(fakeStream);

		Mock<MapTag> mockMapTag = new();
        _mockMapService.Setup(ms => ms.RecentlyAdded).Returns([mockMapTag.Object]);

		LocalizedString existingLocalizedString = new(1001, "Existing Test Category");
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag(1001)).Returns(existingLocalizedString);

		LocalizedString newLocalizedString = new(12345, "Newly Added Songs!");
        _mockLocalizedStringService.Setup(ls => ls.GetAddLocalizedTag("Newly Added Songs!")).Returns(newLocalizedString);

        // Act
        await _service.LoadData();

        // Assert
        Assert.NotNull(_service.ActivityPage);
        Assert.Equal(2, _service.ActivityPage.Categories.Count);

		CarouselCategory? newCategory = _service.ActivityPage.Categories.Values.OfType<CarouselCategory>().FirstOrDefault(c => c.CategoryName == "Newly Added Songs");
        Assert.NotNull(newCategory);
        Assert.Equal(newLocalizedString.OasisIdInt, newCategory.TitleId.ID);

        Assert.Equal(2, _service.ActivityPage.CategoryModifiers.Count);
		PositionModifier firstModifier = Assert.IsType<PositionModifier>(_service.ActivityPage.CategoryModifiers[0]);
        Assert.Equal(0, firstModifier.Position.Position);

        Guid newCategoryGuid = _service.ActivityPage.Categories.First(kvp => kvp.Value == newCategory).Key;
        Assert.Equal(newCategoryGuid, firstModifier.Position.Id);
    }

    [Fact]
    public async Task LoadData_WhenFileDoesNotExist_LogsWarning()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        await _service.LoadData();

        // Assert
        Assert.NotNull(_service.ActivityPage); // Should be the default new()
        Assert.Empty(_service.ActivityPage.Categories); // No data loaded
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("activity-page.json not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadData_WhenJsonIsInvalid_LogsError()
    {
        // Arrange
        string invalidJson = "{ \"categories\": { }";
		MemoryStream fakeStream = new(Encoding.UTF8.GetBytes(invalidJson));

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(fakeStream);
        _mockMapService.Setup(ms => ms.RecentlyAdded).Returns([]);

        // Act
        await _service.LoadData();

        // Assert
        Assert.NotNull(_service.ActivityPage);
        Assert.Empty(_service.ActivityPage.Categories);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load or deserialize activity-page.json")),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadData_WhenDeserializedObjectIsNull_LogsError()
    {
        // Arrange
        string nullJson = "null";
		MemoryStream fakeStream = new(Encoding.UTF8.GetBytes(nullJson));

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns(fakeStream);
        _mockMapService.Setup(ms => ms.RecentlyAdded).Returns([]);

        // Act
        await _service.LoadData();

        // Assert
        Assert.NotNull(_service.ActivityPage);
        Assert.Empty(_service.ActivityPage.Categories);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load or deserialize activity-page.json")),
                It.IsAny<NullReferenceException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}