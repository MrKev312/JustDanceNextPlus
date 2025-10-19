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
		string filePath = Path.Combine(pathSettings.Value.JsonsPath, "activity-page.json");

		if (!fileSystem.FileExists(filePath))
		{
			logger.LogWarning("activity-page.json not found at {Path}, activity page will be unavailable.", filePath);
			return;
		}

		try
		{
			using Stream openStream = fileSystem.OpenRead(filePath);
			ActivityPage = await JsonSerializer.DeserializeAsync<ActivityPageResponse>(openStream, jsonSettingsService.PrettyPascalFormat)
				?? throw new NullReferenceException("Deserialized ActivityPage is null");
            logger.LogInformation("Successfully loaded activity page data.");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to load or deserialize activity-page.json.");
		}

        // If there are no newly added songs, do nothing
		if (mapService.RecentlyAdded.Count == 0)
			return;

        // Create a new category for newly added songs
        Guid newSongsGuid = Guid.NewGuid();
		ActivityPage?.Categories?.Add(newSongsGuid, new CarouselCategory()
		{
			CategoryName = "Newly Added Songs",
			TitleId = localizedStringService.GetAddLocalizedTag("Newly Added Songs!"),
			ItemList = mapService.RecentlyAdded,
			DoNotFilterOwnership = false
		});

        // Create a new modifier to put it on top
		ActivityPage?.CategoryModifiers.Insert(0, new PositionModifier("carousel", newSongsGuid, 0));
    }
}