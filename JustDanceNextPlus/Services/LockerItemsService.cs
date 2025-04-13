using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;

using Microsoft.Extensions.Options;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace JustDanceNextPlus.Services;

public class LockerItemsService(JsonSettingsService jsonSettingsService,
	IOptions<PathSettings> pathSettings,
	ILogger<LockerItemsService> logger)
{
	public Dictionary<string, List<LockerItem>> LockerItems { get; } = [];
	public List<Guid> LockerItemIds { get; } = [];

	public void LoadData()
	{
		string path = pathSettings.Value.LockerItemsPath;

		if (!Directory.Exists(path))
		{
			logger.LogWarning("LockerItems path does not exist, will not load locker items");
			return;
		}

		string[] files = Directory.GetFiles(path, "*.json");

		foreach (string file in files)
		{
			string json = File.ReadAllText(file);
			List<LockerItem> lockerItems = JsonSerializer.Deserialize<List<LockerItem>>(json, jsonSettingsService.PrettyPascalFormat) ?? [];

			if (lockerItems.Count == 0)
			{
				logger.LogWarning("No locker items found in file {File}", file);
				continue;
			}

			string type = lockerItems[0].Type;
			LockerItems[type] = lockerItems;

			foreach (LockerItem lockerItem in lockerItems)
			{
				if (LockerItemIds.Contains(lockerItem.ItemId))
				{
					logger.LogWarning("Duplicate locker item found: {LockerItem}", lockerItem);
					continue;
				}

				LockerItemIds.Add(lockerItem.ItemId);
			}
		}

		logger.LogInformation("Finished loading locker items");
	}
}