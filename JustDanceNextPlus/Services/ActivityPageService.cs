using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class ActivityPageService(ILogger<ActivityPageService> logger,
	IOptions<PathSettings> pathSettings, JsonSettingsService jsonSettingsService,
	MapService mapService, LocalizedStringService localizedStringService) : ILoadService
{
	public ActivityPageResponse ActivityPage { get; private set; } = new();

    public async Task LoadData()
	{
		string filePath = Path.Combine(pathSettings.Value.JsonsPath, "activity-page.json");

		if (!File.Exists(filePath))
		{
			logger.LogWarning("activity-page.json not found at {Path}, activity page will be unavailable.", filePath);
			return;
		}

		try
		{
			using FileStream openStream = File.OpenRead(filePath);
			ActivityPage = await JsonSerializer.DeserializeAsync<ActivityPageResponse>(openStream, jsonSettingsService.PrettyPascalFormat)
				?? throw new NullReferenceException("Deserialized ActivityPage is null");
            logger.LogInformation("Successfully loaded activity page data.");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to load or deserialize activity-page.json.");
		}

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