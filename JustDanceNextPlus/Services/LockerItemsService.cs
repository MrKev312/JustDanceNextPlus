using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public interface ILockerItemsService : ILoadService
{
	Dictionary<string, List<LockerItem>> LockerItems { get; }
	List<Guid> LockerItemIds { get; }
}

public class LockerItemsService(JsonSettingsService jsonSettingsService,
	IOptions<PathSettings> pathSettings,
	ILogger<LockerItemsService> logger,
    IFileSystem fileSystem) : ILockerItemsService, ILoadService
{
	public Dictionary<string, List<LockerItem>> LockerItems { get; } = [];
	public List<Guid> LockerItemIds { get; } = [];

	public async Task LoadData()
	{
		string path = pathSettings.Value.LockerItemsPath;

		if (!fileSystem.DirectoryExists(path))
		{
			logger.LogWarning("LockerItems path does not exist, will not load locker items");
			return;
		}

		string[] files = fileSystem.GetFiles(path, "*.json");

		foreach (string file in files)
		{
			using Stream fileStream = fileSystem.OpenRead(file);
			List<LockerItem> lockerItems = await JsonSerializer.DeserializeAsync<List<LockerItem>>(fileStream, jsonSettingsService.PrettyPascalFormat) ?? [];

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