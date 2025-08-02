using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class ActivityPageService(ILogger<ActivityPageService> logger, IOptions<PathSettings> pathSettings, JsonSettingsService jsonSettingsService) : ILoadService
{
	public ActivityPageResponse? ActivityPage { get; private set; }

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
			ActivityPage = await JsonSerializer.DeserializeAsync<ActivityPageResponse>(openStream, jsonSettingsService.PrettyPascalFormat);
			logger.LogInformation("Successfully loaded activity page data.");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to load or deserialize activity-page.json.");
			ActivityPage = null;
		}
	}
}