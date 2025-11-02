using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;

using Microsoft.Extensions.Options;

using System.Collections.Immutable;
using System.Text.Json;

namespace JustDanceNextPlus.Services;

public interface ILockerItemsService : ILoadService
{
	IReadOnlyDictionary<string, IReadOnlyList<LockerItem>> LockerItems { get; }
	IReadOnlyList<Guid> LockerItemIds { get; }
}

public class LockerItemsService(JsonSettingsService jsonSettingsService,
	IOptions<PathSettings> pathSettings,
	ILogger<LockerItemsService> logger,
    IFileSystem fileSystem) : ILockerItemsService, ILoadService
{
	public IReadOnlyDictionary<string, IReadOnlyList<LockerItem>> LockerItems { get; private set; } = new Dictionary<string, IReadOnlyList<LockerItem>>();
    public IReadOnlyList<Guid> LockerItemIds { get; private set; } = [];

    public async Task LoadData()
	{
		string path = pathSettings.Value.LockerItemsPath;

		if (!fileSystem.DirectoryExists(path))
		{
			logger.LogWarning("LockerItems path does not exist, will not load locker items");
			return;
		}

		string[] files = fileSystem.GetFiles(path, "*.json");

		ImmutableDictionary<string, IReadOnlyList<LockerItem>>.Builder lockerItemsBuilder = ImmutableDictionary.CreateBuilder<string, IReadOnlyList<LockerItem>>();
		ImmutableHashSet<Guid>.Builder lockerItemIdsBuilder = ImmutableHashSet.CreateBuilder<Guid>();

        foreach (string file in files)
		{
			using Stream fileStream = fileSystem.OpenRead(file);
            ImmutableArray<LockerItem> lockerItems = await JsonSerializer.DeserializeAsync<ImmutableArray<LockerItem>>(fileStream, jsonSettingsService.PrettyPascalFormat);

            if (lockerItems.Length == 0)
			{
				logger.LogWarning("No locker items found in file {File}", file);
				continue;
			}

			string type = lockerItems[0].Type;
			lockerItemsBuilder[type] = lockerItems;

			foreach (LockerItem lockerItem in lockerItems)
			{
				if (lockerItemIdsBuilder.Contains(lockerItem.ItemId))
				{
					logger.LogWarning("Duplicate locker item found: {LockerItem}", lockerItem);
					continue;
				}

				lockerItemIdsBuilder.Add(lockerItem.ItemId);
			}
		}

		LockerItems = lockerItemsBuilder.ToImmutable();
		LockerItemIds = lockerItemIdsBuilder.ToImmutableArray();

        logger.LogInformation("Finished loading locker items");
	}
}