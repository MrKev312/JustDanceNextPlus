using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Catalog;

using Microsoft.Extensions.Options;

using System.Collections.ObjectModel;

namespace JustDanceServer.Services;

public interface IJduAliasCatalog : ILoadService
{
	IReadOnlyDictionary<uint, JduAliasDefinition> Aliases { get; }
}

public sealed class JduAliasCatalog(
	IOptions<PathSettings> pathOptions,
	ILogger<JduAliasCatalog> logger) : IJduAliasCatalog
{
	private readonly Lock loadLock = new();
	private readonly JduCatalogAssetDirectory assets = new(pathOptions.Value);
	private Task? loadTask;

	public IReadOnlyDictionary<uint, JduAliasDefinition> Aliases { get
		{
			EnsureLoaded();
			return field;
		}

		private set;
	} = 		new ReadOnlyDictionary<uint, JduAliasDefinition>(new Dictionary<uint, JduAliasDefinition>());

	public Task LoadData()
	{
		lock (loadLock)
			return loadTask ??= LoadDataCore();
	}

	private async Task LoadDataCore()
	{
		Dictionary<uint, JduAliasDefinition> loaded = [];
		foreach (string group in JduCatalogAssetDirectory.GetGroups(assets.GetFamilyRoot("Aliases")))
		{
			JduAliasManifest manifest = await JduCatalogAssetDirectory.LoadManifest<JduAliasManifest>(group, "Alias.json");
			Validate(manifest, group);
			JduCatalogAssetDirectory.RejectUnexpectedFiles(group, [Path.Combine(group, "Alias.json")]);
			if (!loaded.TryAdd(manifest.Id, new(
				manifest.Id,
				manifest.DescriptionLocId,
				manifest.DifficultyColor,
				manifest.RestrictedToUnlimitedSongs,
				manifest.StringLocId,
				manifest.StringLocIdFemale,
				manifest.Placeholder,
				manifest.Localized,
				manifest.LocalizedFemale,
				manifest.Description,
				manifest.Visibility,
				manifest.Unlock)))
			{
				throw new InvalidDataException($"Duplicate JDU alias ID '{manifest.Id}'.");
			}
		}

		Aliases = new ReadOnlyDictionary<uint, JduAliasDefinition>(loaded);
		logger.LogInformation("Loaded {AliasCount} JDU aliases.", loaded.Count);
	}

	private static void Validate(JduAliasManifest alias, string group)
	{
		if (alias.Id == 0xffff)
			throw new InvalidDataException($"JDU alias ID '{alias.Id}' in '{group}' is reserved as invalid.");
		if (alias.DifficultyColor > 5 || alias.Visibility is not (0 or 1))
			throw new InvalidDataException($"JDU alias '{alias.Id}' has invalid color or visibility state.");
		if (string.IsNullOrWhiteSpace(alias.Placeholder)
			|| string.IsNullOrWhiteSpace(alias.Localized)
			|| string.IsNullOrWhiteSpace(alias.LocalizedFemale)
			|| string.IsNullOrWhiteSpace(alias.Description))
		{
			throw new InvalidDataException($"JDU alias '{alias.Id}' has missing display text.");
		}

		if (alias.Unlock is { } unlock
			&& (string.IsNullOrWhiteSpace(unlock.Class) || unlock.MinimumValue < 0))
		{
			throw new InvalidDataException($"JDU alias '{alias.Id}' has an invalid unlock objective.");
		}
	}

	private void EnsureLoaded()
	{
		Task task = LoadData();
		if (!task.IsCompletedSuccessfully)
			task.GetAwaiter().GetResult();
	}
}
