using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JustDanceNextPlus.Tests.Services;

public class LocalizedStringServiceTests
{
    private readonly Mock<ILogger<LocalizedStringService>> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly IOptions<PathSettings> _mockPathSettings;
    private readonly JsonSettingsService _jsonSettingsService;
    private readonly LocalizedStringService _service;

    public LocalizedStringServiceTests()
    {
        _mockLogger = new Mock<ILogger<LocalizedStringService>>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockPathSettings = Options.Create(new PathSettings { JsonsPath = "C:\\jsons" });
        _jsonSettingsService = new JsonSettingsService(new Mock<IServiceProvider>().Object);

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IOptions<PathSettings>)))
                            .Returns(_mockPathSettings);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(JsonSettingsService)))
                            .Returns(_jsonSettingsService);

        _service = new LocalizedStringService(
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _mockFileSystem.Object
        );
    }

    [Fact]
    public async Task LoadData_FileNotFound_LogsInformation()
    {
        // Arrange
        string expectedPath = Path.Combine(_mockPathSettings.Value.JsonsPath, "localizedstrings.json");
        _mockFileSystem.Setup(fs => fs.FileExists(expectedPath)).Returns(false);

        // Act
        await _service.LoadData();

        // Assert
        Assert.Empty(_service.Database.LocalizedStrings);
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Localized strings database not found")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadData_FileFound_LoadsAndSortsDataCorrectly()
    {
		// Arrange
		LocalizedStringDatabase db = new()
		{
            LocalizedStrings =
            [
                new LocalizedString(5000, "Test String B"),
                new LocalizedString(4000, "Test String A")
            ]
        };
		string json = JsonSerializer.Serialize(db, _jsonSettingsService.PrettyPascalFormat);
		MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        string expectedPath = Path.Combine(_mockPathSettings.Value.JsonsPath, "localizedstrings.json");

        _mockFileSystem.Setup(fs => fs.FileExists(expectedPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(expectedPath)).Returns(stream);

        // Act
        await _service.LoadData();

        // Assert
        Assert.Equal(2, _service.Database.LocalizedStrings.Count);
        Assert.Equal(4000, _service.Database.LocalizedStrings[0].OasisIdInt); // Verify it was sorted
        Assert.Equal("Test String A", _service.Database.LocalizedStrings[0].DisplayString);
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Localized strings database loaded")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadData_DeserializationFails_LogsWarning()
    {
        // Arrange
        string invalidJson = "null";
		MemoryStream stream = new(Encoding.UTF8.GetBytes(invalidJson));
        string expectedPath = Path.Combine(_mockPathSettings.Value.JsonsPath, "localizedstrings.json");

        _mockFileSystem.Setup(fs => fs.FileExists(expectedPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(expectedPath)).Returns(stream);

        // Act
        await _service.LoadData();

        // Assert
        Assert.Empty(_service.Database.LocalizedStrings); // Database should not be replaced
        _mockLogger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Localized strings database could not be loaded")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public void GetLocalizedTag_FindsTagInDefaultList()
    {
		// Act
		LocalizedString? tagById = _service.GetLocalizedTag(8747);
		LocalizedString? tagByText = _service.GetLocalizedTag("OK");

        // Assert
        Assert.NotNull(tagById);
        Assert.Equal("Just\u00A0Dance+", tagById.DisplayString);
        Assert.NotNull(tagByText);
        Assert.Equal(1, tagByText.OasisIdInt);
    }

    [Fact]
    public async Task GetLocalizedTag_FindsTagInDatabaseList()
    {
		// Arrange
		Guid guid = Guid.NewGuid();
		LocalizedStringDatabase db = new()
		{
            LocalizedStrings = [new() { OasisIdInt = 9999, DisplayString = "Database String", LocalizedStringId = guid }]
        };
		string json = JsonSerializer.Serialize(db, _jsonSettingsService.PrettyPascalFormat);
		MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        string path = Path.Combine(_mockPathSettings.Value.JsonsPath, "localizedstrings.json");
        _mockFileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(path)).Returns(stream);
        await _service.LoadData();

		// Act
		LocalizedString? tagById = _service.GetLocalizedTag(9999);
		LocalizedString? tagByText = _service.GetLocalizedTag("Database String");
		LocalizedString? tagByGuid = _service.GetLocalizedTag(guid);

        // Assert
        Assert.NotNull(tagById);
        Assert.Equal("Database String", tagById.DisplayString);
        Assert.NotNull(tagByText);
        Assert.Equal(9999, tagByText.OasisIdInt);
        Assert.NotNull(tagByGuid);
        Assert.Equal(9999, tagByGuid.OasisIdInt);
    }

    [Fact]
    public void GetAddLocalizedTag_ReturnsExistingTagWithoutAdding()
    {
        // Arrange
        _service.Database.LocalizedStrings.Add(new LocalizedString(10000, "Existing DB String"));

		// Act
		LocalizedString resultFromDefault = _service.GetAddLocalizedTag("Yes");
		LocalizedString resultFromDb = _service.GetAddLocalizedTag("Existing DB String");

        // Assert
        Assert.Equal(2, resultFromDefault.OasisIdInt);
        Assert.Equal(10000, resultFromDb.OasisIdInt);
        Assert.Single(_service.Database.LocalizedStrings); // Verify no new tags were added
    }

    [Fact]
    public void GetAddLocalizedTag_AddsNewTagWithIncrementedId()
    {
        // Arrange
        _service.Database.LocalizedStrings.Add(new LocalizedString(10000, "Highest ID String"));

		// Act
		LocalizedString newTag = _service.GetAddLocalizedTag("A Brand New String");

        // Assert
        Assert.NotNull(newTag);
        Assert.Equal("A Brand New String", newTag.DisplayString);
        Assert.Equal(10001, newTag.OasisIdInt);

		LocalizedString? foundTag = _service.GetLocalizedTag("A Brand New String");
        Assert.NotNull(foundTag);
        Assert.Equal(10001, foundTag.OasisIdInt);
    }

    [Fact]
    public void GetAddLocalizedTag_ThrowsWhenDatabaseIsEmpty()
    {
        // Assert
        Assert.Throws<InvalidOperationException>(() => _service.GetAddLocalizedTag("First Ever String"));
    }

    [Fact]
    public async Task GetAddLocalizedTag_IsThreadSafe()
    {
        // Arrange
        _service.Database.LocalizedStrings.Add(new LocalizedString(10000, "Highest ID String"));

		// Act
		List<Task<LocalizedString>> tasks = [.. Enumerable.Range(0, 10).Select(_ => Task.Run(() => _service.GetAddLocalizedTag("Thread Safe Test")))];

		LocalizedString[] results = await Task.WhenAll(tasks);

		// Assert
		int firstId = results[0].OasisIdInt;
        Assert.All(results, result => Assert.Equal(firstId, result.OasisIdInt));

		int countInDb = _service.Database.LocalizedStrings.Count(s => s.DisplayString == "Thread Safe Test");
        Assert.Equal(1, countInDb);
    }
}