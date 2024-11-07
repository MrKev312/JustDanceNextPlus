using JustDanceNextPlus.Configuration;

using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Services;

public class LocalizedStringService
{
	public LocalizedStringDatabase Database { get; } = new();

	readonly ConcurrentDictionary<string, int> localizedTags = new();
	readonly ConcurrentDictionary<Guid, int> localizedTagsGuid = new();

	public LocalizedStringService(ILogger<LocalizedStringService> logger,
		IOptions<PathSettings> settings)
	{
		string path = Path.Combine(settings.Value.JsonsPath, "localizedstrings.json");
		if (!File.Exists(path))
		{
			logger.LogInformation("Localized strings database not found, creating a new one");
			return;
		}

		string json = File.ReadAllText(path);
		LocalizedStringDatabase? db = JsonSerializer.Deserialize<LocalizedStringDatabase>(json, JsonSettings.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Localized strings database could not be loaded");
			return;
		}

		Database = db;
		logger.LogInformation("Localized strings database loaded");

		// Populate the dictionaries
		Parallel.For(0, Database.LocalizedStrings.Count, i =>
		{
			LocalizedString localizedString = Database.LocalizedStrings[i];
			localizedTags.TryAdd(localizedString.DisplayString, i);
			localizedTagsGuid.TryAdd(localizedString.LocalizedStringId, i);
		});
	}

	public LocalizedString? GetLocalizedTag(string text)
	{
		if (localizedTags.TryGetValue(text, out int index))
			return Database.LocalizedStrings[index];

		return null;
	}

	public LocalizedString? GetLocalizedTag(Guid id)
	{
		if (localizedTagsGuid.TryGetValue(id, out int index))
			return Database.LocalizedStrings[index];

		return null;
	}

	public LocalizedString? GetLocalizedTag(int id)
	{
		if (id < Database.LocalizedStrings.Count)
			return Database.LocalizedStrings[id];

		return null;
	}

	public LocalizedString GetAddLocalizedTag(string text)
	{
		// If it contains the tag, return it
		LocalizedString? localizedTag = GetLocalizedTag(text);
		if (localizedTag != null)
			return localizedTag;

		// Lock the list
		lock (Database.LocalizedStrings)
		{
			// Check again
			localizedTag = GetLocalizedTag(text);
			if (localizedTag != null)
				return localizedTag;

			// Generate a new GUID
			Guid guid;
			do
			{
				guid = Guid.NewGuid();
			}
			while (localizedTagsGuid.ContainsKey(guid));

			int index = Database.LocalizedStrings.Count;

			localizedTag = new()
			{
				OasisIdInt = index + 10000,
				LocaleCode = "en-US",
				DisplayString = text,
				LocalizedStringId = guid
			};

			Database.LocalizedStrings.Add(localizedTag);
			localizedTags.TryAdd(text, index);
			localizedTagsGuid.TryAdd(guid, index);
			return localizedTag;
		}
	}
}

public class LocalizedStringDatabase
{
	public Guid SpaceId { get; set; } = Guid.Parse("1da01a17-3bc7-4b5d-aedd-70a0915089b0");
	public List<LocalizedString> LocalizedStrings { get; set; } = [];
}

public class LocalizedString
{
	[JsonIgnore]
	public int OasisIdInt { get; set; }
	public string OasisId { get => OasisIdInt.ToString(); set => OasisIdInt = int.Parse(value); }
	public string LocaleCode { get; set; } = "";
	public string DisplayString { get; set; } = "";
	public Guid LocalizedStringId { get; set; }
	public object? Obj { get; set; }
	public int SpaceRevision { get; set; } = 121;
}