using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using System.Text;
using System.Text.Json;

namespace JustDanceNextPlus.Tests.Services;

public class TagServiceTests
{
    private readonly Mock<ILocalizedStringService> _mockLocalizedStringService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<TagService>> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly IOptions<PathSettings> _mockPathSettings;
    private readonly JsonSettingsService _jsonSettingsService;
    private readonly TagService _service;

    public TagServiceTests()
    {
        _mockLocalizedStringService = new Mock<ILocalizedStringService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<TagService>>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockPathSettings = Options.Create(new PathSettings { JsonsPath = "C:\\jsons" });
        _jsonSettingsService = new JsonSettingsService(new Mock<IServiceProvider>().Object);

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IOptions<PathSettings>)))
                            .Returns(_mockPathSettings);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(JsonSettingsService)))
                            .Returns(_jsonSettingsService);

        _service = new TagService(
            _mockLocalizedStringService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockFileSystem.Object
        );
    }

    [Fact]
    public async Task LoadData_FileExists_LoadsAndInitializesTags()
    {
		// Arrange
		Guid tagGuid = Guid.NewGuid();
		TagDatabase db = new()
		{
            Tags = new OrderedDictionary<Guid, Tag>
            {
                [tagGuid] = new Tag { TagName = "TestTag", LocId = new OasisTag(new LocalizedString(123, "TestTag")) }
            }
        };
		string json = JsonSerializer.Serialize(db, _jsonSettingsService.PrettyPascalFormat);
		MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        string path = Path.Combine(_mockPathSettings.Value.JsonsPath, "tagdb.json");

        _mockFileSystem.Setup(fs => fs.FileExists(path)).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenRead(path)).Returns(stream);

        // Act
        await _service.LoadData();

        // Assert
        Assert.Single(_service.TagDatabase.Tags);
		Tag loadedTag = _service.TagDatabase.Tags[tagGuid];
        Assert.Equal(tagGuid, loadedTag.TagGuid); // Verify the Guid was set from the key
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tag database loaded")), null, It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once
        );
    }

    [Fact]
    public void GetTag_ByGuid_ReturnsCorrectTag()
    {
		// Arrange
		Guid tagGuid = Guid.NewGuid();
		Tag expectedTag = new() { TagGuid = tagGuid, TagName = "Test", LocId = new OasisTag(new LocalizedString(123, "Test")) };
        _service.TagDatabase.Tags[tagGuid] = expectedTag;

		// Act
		Tag? result = _service.GetTag(tagGuid);

        // Assert
        Assert.Equal(expectedTag, result);
    }

    [Fact]
    public void GetTag_ByText_WhenTagExists_ReturnsTag()
    {
		// Arrange
		LocalizedString locString = new(123, "Test Tag");
		Tag expectedTag = new() { LocId = new OasisTag(locString) };
        _service.TagDatabase.Tags[Guid.NewGuid()] = expectedTag;
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag("Test Tag")).Returns(locString);

		// Act
		Tag? result = _service.GetTag("Test Tag");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTag, result);
    }

    [Fact]
    public void GetTag_ByText_WhenTagDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag("NonExistent")).Returns((LocalizedString?)null);

		// Act
		Tag? result = _service.GetTag("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAddTag_WhenTagExists_ReturnsExistingTag()
    {
		// Arrange
		LocalizedString locString = new(1, "Existing Tag");
		Tag existingTag = new() { LocId = new OasisTag(locString) };
        _service.TagDatabase.Tags[Guid.NewGuid()] = existingTag;
        _mockLocalizedStringService.Setup(ls => ls.GetAddLocalizedTag("Existing Tag")).Returns(locString);
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag("Existing Tag")).Returns(locString);

		// Act
		Tag result = _service.GetAddTag("Existing Tag", "any_category");

        // Assert
        Assert.Equal(existingTag, result);
        Assert.Single(_service.TagDatabase.Tags); // No new tag should be added
    }

    [Fact]
    public void GetAddTag_WhenTagDoesNotExist_CreatesNewTag()
    {
		// Arrange
		LocalizedString locString = new(1, "New Tag");
        _mockLocalizedStringService.Setup(ls => ls.GetAddLocalizedTag("New Tag")).Returns(locString);
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag("New Tag")).Returns((LocalizedString?)null);

		// Act
		Tag result = _service.GetAddTag("New Tag", "mood");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Tag", result.TagName);
        Assert.Equal("mood", result.Category);
        Assert.Single(_service.TagDatabase.Tags);
        Assert.Contains(result.TagGuid, _service.TagDatabase.IsPresentInSongLibrary);
        Assert.Contains(result.TagGuid, _service.TagDatabase.IsPresentInSongPageDetails);
    }

    [Fact]
    public void GetAddTag_ForArtistCategory_DoesNotAddToLibraryLists()
    {
		// Arrange
		LocalizedString locString = new(1, "New Artist");
        _mockLocalizedStringService.Setup(ls => ls.GetAddLocalizedTag("New Artist")).Returns(locString);
        _mockLocalizedStringService.Setup(ls => ls.GetLocalizedTag("New Artist")).Returns((LocalizedString?)null);

		// Act
		Tag result = _service.GetAddTag("New Artist", "artist");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("artist", result.Category);
        Assert.DoesNotContain(result.TagGuid, _service.TagDatabase.IsPresentInSongLibrary);
        Assert.DoesNotContain(result.TagGuid, _service.TagDatabase.IsPresentInSongPageDetails);
    }

    [Fact]
    public void GetFilters_ReturnsCorrectlyStructuredAndOrderedFilters()
    {
		// Arrange
		Guid soloGuid = Guid.NewGuid();
		Guid duoGuid = Guid.NewGuid();
		Guid popGuid = Guid.NewGuid();
		Guid rockGuid = Guid.NewGuid();
        _service.TagDatabase.Tags = new OrderedDictionary<Guid, Tag>
        {
            [soloGuid] = new Tag { TagName = "Solo", Category = "choreoSettings", LocId = new OasisTag(new LocalizedString(1, "Solo")) },
            [duoGuid] = new Tag { TagName = "Duo", Category = "choreoSettings", LocId = new OasisTag(new LocalizedString(2, "Duo")) },
            [popGuid] = new Tag { TagName = "Pop", Category = "musicalGenre", LocId = new OasisTag(new LocalizedString(3, "Pop")) },
            [rockGuid] = new Tag { TagName = "Rock", Category = "musicalGenre", LocId = new OasisTag(new LocalizedString(4, "Rock")) },
        };

		// Act
		List<Filter> filters = _service.GetFilters();

        // Assert
        Assert.Equal(5, filters.Count);

		Filter? choreoFilter = filters.FirstOrDefault(f => f.Category == "choreoSettings");
        Assert.NotNull(choreoFilter);
        Assert.Equal(318, choreoFilter.LocId);
        Assert.Equal(soloGuid, choreoFilter.Order[0]);
        Assert.Equal(duoGuid, choreoFilter.Order[1]);

		Filter? genreFilter = filters.FirstOrDefault(f => f.Category == "musicalGenre");
        Assert.NotNull(genreFilter);
        Assert.Equal(317, genreFilter.LocId);
        Assert.Equal(popGuid, genreFilter.Order[0]); // Verifies alphabetical order by TagName
        Assert.Equal(rockGuid, genreFilter.Order[1]);
    }
}