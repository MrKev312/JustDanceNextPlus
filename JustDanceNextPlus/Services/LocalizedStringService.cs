using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public interface ILocalizedStringService : ILoadService
{
	LocalizedStringDatabase Database { get; }

    LocalizedString? GetLocalizedTag(string text);
    LocalizedString? GetLocalizedTag(Guid id);
    LocalizedString? GetLocalizedTag(int id);
    LocalizedString GetAddLocalizedTag(string text);
}

public class LocalizedStringService(ILogger<LocalizedStringService> logger,
	IServiceProvider serviceProvider,
    IFileSystem fileSystem) : ILocalizedStringService, ILoadService
{
	public LocalizedStringDatabase Database { get; private set; } = new();
	int? highestId = null;

	// Localization strings that are only available in the game and are not needed to be localized
	// TODO: Add all missing localized strings from the game
	static readonly List<LocalizedString> defaultLocalizedStrings =
	[
		new LocalizedString(0, ""),
		new LocalizedString(1, "OK"),
		new LocalizedString(2, "Yes"),
		new LocalizedString(3, "Continue"),
		new LocalizedString(4, "Back"),
		new LocalizedString(23, "Playlists we think you'll like"),
		new LocalizedString(25, "Songs you might like"),
		new LocalizedString(26, "Everybody loves [VAR:TAGNAME]"),
		new LocalizedString(28, "Everybody's playing them"),
		new LocalizedString(30, "Sweat Longer"),
		new LocalizedString(32, "No"),
		new LocalizedString(33, "Cancel"),
		new LocalizedString(8747, "Just\u00A0Dance+")
	];

	public async Task LoadData()
	{
		IOptions<PathSettings> settings = serviceProvider.GetRequiredService<IOptions<PathSettings>>();
		JsonSettingsService jsonSettingsService = serviceProvider.GetRequiredService<JsonSettingsService>();

		string path = Path.Combine(settings.Value.JsonsPath, "localizedstrings.json");
		if (!fileSystem.FileExists(path))
		{
			logger.LogInformation("Localized strings database not found, creating a new one");
			return;
		}

		using Stream fileStream = fileSystem.OpenRead(path);
		LocalizedStringDatabase? db = await JsonSerializer.DeserializeAsync<LocalizedStringDatabase>(fileStream, jsonSettingsService.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Localized strings database could not be loaded");
			return;
		}

		// Sort the localized strings by OasisIdInt
		db.LocalizedStrings = [.. db.LocalizedStrings.OrderBy(ls => ls.OasisId)];

		Database = db;
		logger.LogInformation("Localized strings database loaded");
	}

	public LocalizedString? GetLocalizedTag(string text)
	{
		return defaultLocalizedStrings
			.FirstOrDefault(ls => ls.DisplayString == text)
			?? Database.LocalizedStrings
			.FirstOrDefault(ls => ls.DisplayString == text);
	}

	public LocalizedString? GetLocalizedTag(Guid id)
	{
		return defaultLocalizedStrings
			.FirstOrDefault(ls => ls.LocalizedStringId == id)
			?? Database.LocalizedStrings
			.FirstOrDefault(ls => ls.LocalizedStringId == id);
	}

	public LocalizedString? GetLocalizedTag(int id)
	{
		return defaultLocalizedStrings
			.FirstOrDefault(ls => ls.OasisId == id)
			?? Database.LocalizedStrings
			.FirstOrDefault(ls => ls.OasisId == id);
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

			highestId ??= Database.LocalizedStrings.Max(ls => ls.OasisId); // Get the highest ID in the database
			highestId++; // Increment it for the new localized string

			localizedTag = new()
			{
				OasisId = highestId.Value,
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
