using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using System.Text;

namespace JustDanceNextPlus.Tests.Services;

public class LockerItemsServiceTests
{
    private readonly Mock<ILogger<LockerItemsService>> _mockLogger;
    private readonly IOptions<PathSettings> _mockPathSettings;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly JsonSettingsService _jsonSettingsService;
    private readonly LockerItemsService _service;

    public LockerItemsServiceTests()
    {
        _mockLogger = new Mock<ILogger<LockerItemsService>>();
        _mockPathSettings = Options.Create(new PathSettings { LockerItemsPath = "C:\\lockers" });
        _mockFileSystem = new Mock<IFileSystem>();

		Mock<IServiceProvider> mockServiceProvider = new();
        _jsonSettingsService = new JsonSettingsService(mockServiceProvider.Object);

        _service = new LockerItemsService(
            _jsonSettingsService,
            _mockPathSettings,
            _mockLogger.Object,
            _mockFileSystem.Object
        );
    }

    [Fact]
    public async Task LoadData_DirectoryDoesNotExist_LogsWarningAndReturns()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

        // Act
        await _service.LoadData();

        // Assert
        Assert.Empty(_service.LockerItems);
        Assert.Empty(_service.LockerItemIds);
        _mockLogger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LockerItems path does not exist")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadData_LoadsItemsFromMultipleFiles()
    {
		// Arrange
		Guid avatarId = Guid.NewGuid();
		Guid skinId = Guid.NewGuid();
        string avatarsJson = $$"""
        [
          { "itemId": "{{avatarId}}", "type": "avatar", "nameId": "avatar_test" }
        ]
        """;
        string skinsJson = $$"""
        [
          { "itemId": "{{skinId}}", "type": "skin", "nameId": "skin_test" }
        ]
        """;

        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.json", SearchOption.TopDirectoryOnly))
                       .Returns(["avatars.json", "skins.json"]);
        _mockFileSystem.Setup(fs => fs.OpenRead("avatars.json")).Returns(new MemoryStream(Encoding.UTF8.GetBytes(avatarsJson)));
        _mockFileSystem.Setup(fs => fs.OpenRead("skins.json")).Returns(new MemoryStream(Encoding.UTF8.GetBytes(skinsJson)));

        // Act
        await _service.LoadData();

        // Assert
        Assert.Equal(2, _service.LockerItems.Count);
        Assert.True(_service.LockerItems.ContainsKey("avatar"));
        Assert.True(_service.LockerItems.ContainsKey("skin"));
        Assert.Single(_service.LockerItems["avatar"]);
        Assert.Equal(avatarId, _service.LockerItems["avatar"][0].ItemId);
        Assert.Contains(avatarId, _service.LockerItemIds);
        Assert.Contains(skinId, _service.LockerItemIds);
    }

    [Fact]
    public async Task LoadData_LogsWarningForDuplicateLockerItemId()
    {
		// Arrange
		Guid duplicateId = Guid.NewGuid();
        string avatarsJson = $$"""[ { "itemId": "{{duplicateId}}", "type": "avatar", "nameId": "avatar_test" } ]""";
        string skinsJson = $$"""[ { "itemId": "{{duplicateId}}", "type": "skin", "nameId": "skin_test" } ]""";

        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.json", SearchOption.TopDirectoryOnly)).Returns(["avatars.json", "skins.json"]);
        _mockFileSystem.Setup(fs => fs.OpenRead("avatars.json")).Returns(new MemoryStream(Encoding.UTF8.GetBytes(avatarsJson)));
        _mockFileSystem.Setup(fs => fs.OpenRead("skins.json")).Returns(new MemoryStream(Encoding.UTF8.GetBytes(skinsJson)));

        // Act
        await _service.LoadData();

        // Assert
        Assert.Single(_service.LockerItemIds); // Should only contain one instance of the ID
        _mockLogger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Duplicate locker item found")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadData_HandlesEmptyJsonFile()
    {
        // Arrange
        string emptyJson = "[]";
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.json", SearchOption.TopDirectoryOnly)).Returns(["empty.json"]);
        _mockFileSystem.Setup(fs => fs.OpenRead("empty.json")).Returns(new MemoryStream(Encoding.UTF8.GetBytes(emptyJson)));

        // Act
        await _service.LoadData();

        // Assert
        Assert.Empty(_service.LockerItems);
        _mockLogger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No locker items found in file")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}