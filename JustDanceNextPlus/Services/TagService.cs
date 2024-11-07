using HotChocolate.Execution;

using JustDanceNextPlus.Configuration;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class TagService
{
	private readonly LocalizedStringService localizedStringService;

	public TagService(LocalizedStringService localizedStringService,
		ILogger<TagService> logger,
		IOptions<PathSettings> pathSettings)
	{
		this.localizedStringService = localizedStringService;

		// Load the tags
		string tagsPath = Path.Combine(pathSettings.Value.JsonsPath, "tagdb.json");
		if (!File.Exists(tagsPath))
		{
			logger.LogInformation("Tag database not found, creating a new one");
			return;
		}

		string json = File.ReadAllText(tagsPath);
		TagDatabase? db = JsonSerializer.Deserialize<TagDatabase>(json, JsonSettings.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Tag database could not be loaded");
			return;
		}

		TagDatabase = db;
		logger.LogInformation("Tag database loaded");
	}

	public TagDatabase TagDatabase { get; } = new();

	public Guid? GetTag(string text)
	{
		LocalizedString? localizedTag = localizedStringService.GetLocalizedTag(text);

		if (localizedTag == null)
			return null;

		Guid tagGuid = TagDatabase.Tags.FirstOrDefault(x => x.Value.LocId == localizedTag.OasisIdInt).Key;

		if (tagGuid == Guid.Empty)
			return null;

		return tagGuid;
	}

	public Guid GetAddTag(string text, string category)
	{
		LocalizedString localizedTag = localizedStringService.GetAddLocalizedTag(text);

		// If it contains the tag, return it
		Guid? tag = GetTag(localizedTag.DisplayString);

		if (tag != null)
			return tag.Value;

		// Lock the list
		lock (TagDatabase.Tags)
		{
			// Check again
			tag = GetTag(localizedTag.DisplayString);

			if (tag != null)
				return tag.Value;

			Tag newTag = new()
			{
				TagName = localizedTag.DisplayString,
				LocId = localizedTag.OasisIdInt,
				Category = category
			};

			TagDatabase.Tags.Add(localizedTag.LocalizedStringId, newTag);

			if (category != "artist")
			{
				TagDatabase.IsPresentInSongLibrary.Add(localizedTag.LocalizedStringId);
				TagDatabase.IsPresentInSongPageDetails.Add(localizedTag.LocalizedStringId);
			}

			return localizedTag.LocalizedStringId;
		}
	}
}

public class TagDatabase
{
	public OrderedDictionary<Guid, Tag> Tags { get; set; } = [];
	public List<Guid> IsPresentInSongLibrary { get; set; } = [];
	public List<Guid> IsPresentInSongPageDetails { get; set; } = [];
}

public class Tag
{
	public string TagName { get; set; } = "";
	public int LocId { get; set; }
	public string Category { get; set; } = "";
	public List<string> Synonyms { get; set; } = [];
}
