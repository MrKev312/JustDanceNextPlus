using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class LocalizedStringService(ILogger<LocalizedStringService> logger,
	IServiceProvider serviceProvider)
{
	public LocalizedStringDatabase Database { get; private set; } = new();

	public void LoadData()
	{
		IOptions<PathSettings> settings = serviceProvider.GetRequiredService<IOptions<PathSettings>>();
		JsonSettingsService jsonSettingsService = serviceProvider.GetRequiredService<JsonSettingsService>();

		string path = Path.Combine(settings.Value.JsonsPath, "localizedstrings.json");
		if (!File.Exists(path))
		{
			logger.LogInformation("Localized strings database not found, creating a new one");
			return;
		}

		string json = File.ReadAllText(path);
		LocalizedStringDatabase? db = JsonSerializer.Deserialize<LocalizedStringDatabase>(json, jsonSettingsService.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Localized strings database could not be loaded");
			return;
		}

		// Add 1-33 as these are only in game and not in the server json
		Dictionary<int, string> defaultLocalizedStrings = new()
		{
			{ 0, "" }, // 0 means display nothing
  			{ 1, "OK" },
			{ 2, "Yes" },
			{ 3, "Continue" },
			{ 4, "Back" },
			{ 23, "Playlists we think you'll like" },
			{ 25, "Songs you might like" },
			{ 26, "Everybody loves [VAR:TAGNAME]" },
			{ 28, "Everybody's playing them" },
			{ 30, "Sweat Longer" },
			{ 32, "No" },
			{ 33, "Cancel" }
		};

		foreach (var kvp in defaultLocalizedStrings)
		{
			if (db.LocalizedStrings.Any(ls => ls.OasisIdInt == kvp.Key))
				continue;
			db.LocalizedStrings.Add(new LocalizedString
			{
				OasisIdInt = kvp.Key,
				LocaleCode = "en-US",
				DisplayString = kvp.Value,
				LocalizedStringId = Guid.NewGuid()
			});
		}

		// Sort the localized strings by OasisIdInt
		db.LocalizedStrings = [.. db.LocalizedStrings.OrderBy(ls => ls.OasisIdInt)];

		Database = db;
		logger.LogInformation("Localized strings database loaded");
	}

	public LocalizedString? GetLocalizedTag(string text)
	{
		return Database.LocalizedStrings
			.Where(ls => ls.DisplayString == text)
			.FirstOrDefault();
	}

	public LocalizedString? GetLocalizedTag(Guid id)
	{
		return Database.LocalizedStrings
			.Where(ls => ls.LocalizedStringId == id)
			.FirstOrDefault();
	}

	public LocalizedString? GetLocalizedTag(int id)
	{
		return Database.LocalizedStrings
			.Where(ls => ls.OasisIdInt == id)
			.FirstOrDefault();
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
			while (Database.LocalizedStrings.Any(ls => ls.LocalizedStringId == guid));

			int index = Database.LocalizedStrings.Count;

			localizedTag = new()
			{
				OasisIdInt = index + 10000,
				LocaleCode = "en-US",
				DisplayString = text,
				LocalizedStringId = guid
			};

			Database.LocalizedStrings.Add(localizedTag);

			return localizedTag;
		}
	}
}

public class LocalizedStringDatabase
{
	public Guid SpaceId { get; set; } = Guid.Parse("1da01a17-3bc7-4b5d-aedd-70a0915089b0");
	public List<LocalizedString> LocalizedStrings { get; set; } = [];
}
