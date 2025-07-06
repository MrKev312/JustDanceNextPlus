using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class TagService(LocalizedStringService localizedStringService, IServiceProvider serviceProvider, ILogger<TagService> logger)
{
	public TagDatabase TagDatabase { get; private set; } = new();

	public void LoadData()
	{
		IOptions<PathSettings> pathSettings = serviceProvider.GetRequiredService<IOptions<PathSettings>>();
		JsonSettingsService jsonSettingsService = serviceProvider.GetRequiredService<JsonSettingsService>();

		// Load the tags
		string tagsPath = Path.Combine(pathSettings.Value.JsonsPath, "tagdb.json");
		if (!File.Exists(tagsPath))
		{
			logger.LogInformation("Tag database not found, creating a new one");
			return;
		}

		string json = File.ReadAllText(tagsPath);
		TagDatabase? db = JsonSerializer.Deserialize<TagDatabase>(json, jsonSettingsService.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Tag database could not be loaded");
			return;
		}

		foreach (KeyValuePair<Guid, Tag> tag in db.Tags)
			tag.Value.TagGuid = tag.Key;

		TagDatabase = db;
		logger.LogInformation("Tag database loaded");
	}

	public Tag? GetTag(string text)
	{
		LocalizedString? localizedTag = localizedStringService.GetLocalizedTag(text);

		if (localizedTag == null)
			return null;

		Tag tagGuid;
		lock (TagDatabase.Tags)
			tagGuid = TagDatabase.Tags.FirstOrDefault(x => x.Value.LocId == localizedTag.OasisIdInt).Value;

		return tagGuid;
	}

	public Tag? GetTag(Guid tagGuid)
	{
		TagDatabase.Tags.TryGetValue(tagGuid, out Tag? tag);
		return tag;
	}

	public Tag GetAddTag(string text, string category)
	{
		LocalizedString localizedTag = localizedStringService.GetAddLocalizedTag(text);

		// If it contains the tag, return it
		Tag? tag = GetTag(localizedTag.DisplayString);

		if (tag != null)
			return tag;

		// Lock the list
		lock (TagDatabase.Tags)
		{
			// Check again
			tag = GetTag(localizedTag.DisplayString);

			if (tag != null)
				return tag;

			// Generate an unused guid
			Guid tagGuid = Guid.NewGuid();
			while (TagDatabase.Tags.ContainsKey(tagGuid))
			{
				tagGuid = Guid.NewGuid();
			}

			Tag newTag = new()
			{
				TagGuid = tagGuid,
				TagName = localizedTag.DisplayString,
				LocId = new(localizedTag),
				Category = category
			};

			TagDatabase.Tags.Add(tagGuid, newTag);

			if (category != "artist")
			{
				TagDatabase.IsPresentInSongLibrary.Add(tagGuid);
				TagDatabase.IsPresentInSongPageDetails.Add(tagGuid);
			}

			return newTag;
		}
	}

	internal List<Filter> GetFilters()
	{
		List<Filter> filters = [];

		// First we get the choreo filters
		string[] choreoFilters = ["Solo", "Duo", "Trio", "Quatuor", "Kids", "For Friends"];
		filters.Add(new()
		{
			LocId = 318,
			Order = [.. choreoFilters.Select(x => TagDatabase.Tags.FirstOrDefault(y => y.Value.TagName == x).Key)],
			Category = "choreoSettings"
		});

		// Musical genre filters
		filters.Add(new()
		{
			LocId = 317,
			Order = [.. TagDatabase.Tags.Where(x => x.Value.Category == "musicalGenre").OrderBy(x => x.Value.TagName).Select(x => x.Key)],
			Category = "musicalGenre"
		});

		// Mood filters
		filters.Add(new()
		{
			LocId = 319,
			Order = [.. TagDatabase.Tags.Where(x => x.Value.Category == "mood").OrderBy(x => x.Value.TagName).Select(x => x.Key)],
			Category = "mood"
		});

		// Decade filters
		filters.Add(new()
		{
			LocId = 320,
			Order = [.. TagDatabase.Tags.Where(x => x.Value.Category == "decades").OrderBy(x => x.Value.TagName).Select(x => x.Key)],
			Category = "decades"
		});

		// Accesibility filters
		filters.Add(new()
		{
			LocId = 321,
			Order = [.. TagDatabase.Tags.Where(x => x.Value.Category == "accessibility").OrderBy(x => x.Value.TagName).Select(x => x.Key)],
			Category = "accessibility"
		});

		return filters;
	}
}

public class TagDatabase
{
	public OrderedDictionary<Guid, Tag> Tags { get; set; } = [];
	public List<Guid> IsPresentInSongLibrary { get; set; } = [];
	public List<Guid> IsPresentInSongPageDetails { get; set; } = [];
}

public class Filter
{
	public int LocId { get; set; }
	public List<Guid> Order { get; set; } = [];
	public string Category { get; set; } = "";
}
