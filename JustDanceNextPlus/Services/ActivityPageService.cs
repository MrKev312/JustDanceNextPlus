using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public interface IActivityPageService : ILoadService
{
    ActivityPageResponse ActivityPage { get; }
}

public class ActivityPageService(ILogger<ActivityPageService> logger,
    IOptions<PathSettings> pathSettings, JsonSettingsService jsonSettingsService,
    IMapService mapService, ILocalizedStringService localizedStringService,
    IFileSystem fileSystem) : IActivityPageService, ILoadService
{
    public ActivityPageResponse ActivityPage { get; private set; } = new();

    public async Task LoadData()
    {
		// First, load the base activity page from the file.
		ActivityPageResponse? loadedActivityPage = await LoadBaseActivityPageAsync();

        // If loading failed or there are no recently added songs, assign the loaded page and we're done.
        if (loadedActivityPage == null || mapService.RecentlyAdded.Count == 0)
        {
            ActivityPage = loadedActivityPage ?? new ActivityPageResponse();
            return;
        }

        // Since we have new songs, we'll create a new, modified version of the activity page.
        // This is a pure function that takes the base page and returns an updated one.
        ActivityPage = CreatePageWithNewlyAddedSongs(loadedActivityPage);
    }

    private async Task<ActivityPageResponse?> LoadBaseActivityPageAsync()
    {
        string filePath = Path.Combine(pathSettings.Value.JsonsPath, "activity-page.json");

        if (!fileSystem.FileExists(filePath))
        {
            logger.LogWarning("activity-page.json not found at {Path}, activity page will be unavailable.", filePath);
            return null;
        }

        try
        {
            using Stream openStream = fileSystem.OpenRead(filePath);
			ActivityPageResponse? activityPage = await JsonSerializer.DeserializeAsync<ActivityPageResponse>(openStream, jsonSettingsService.PrettyPascalFormat)
                ?? throw new NullReferenceException("Deserialized ActivityPage is null");
			logger.LogInformation("Successfully loaded activity page data.");
            return activityPage;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load or deserialize activity-page.json.");
            return null;
        }
    }

    private ActivityPageResponse CreatePageWithNewlyAddedSongs(ActivityPageResponse basePage)
    {
        // Create a new category for the newly added songs
        Guid newSongsGuid = Guid.NewGuid();
		CarouselCategory newCategory = new()
        {
            CategoryName = "Newly Added Songs",
            TitleId = localizedStringService.GetAddLocalizedTag("Newly Added Songs!"),
            ItemList = [.. mapService.RecentlyAdded],
            DoNotFilterOwnership = false
        };

		// Create a new modifier to place this category at the top
		PositionModifier newModifier = new("carousel", newSongsGuid, 0);

        // Create new immutable collections by adding our new items to the existing ones.
        var updatedCategories = basePage.Categories.Add(newSongsGuid, newCategory);
        var updatedModifiers = basePage.CategoryModifiers.Insert(0, newModifier);

        // Use a 'with' expression to create a new, updated ActivityPageResponse.
        return basePage with
        {
            Categories = updatedCategories,
            CategoryModifiers = updatedModifiers
        };
    }
}