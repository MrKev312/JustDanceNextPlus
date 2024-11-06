using HotChocolate.Execution;

namespace JustDanceNextPlus.Services;

public class TagService(LocalizedStringService localizedStringService)
{
	public OrderedDictionary<Guid, Tag> Tags { get; } = [];
	public List<Guid> IsPresentInSongLibrary { get; } = [];
	public List<Guid> IsPresentInSongPageDetails { get; } = [];

	public Guid? GetTag(string text)
	{
		LocalizedString? localizedTag = localizedStringService.GetLocalizedTag(text);

		if (localizedTag == null)
			return null;

		Guid tagGuid = Tags.FirstOrDefault(x => x.Value.LocId == localizedTag.OasisIdInt).Key;

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
		lock (Tags)
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

			Tags.Add(localizedTag.LocalizedStringId, newTag);

			IsPresentInSongLibrary.Add(localizedTag.LocalizedStringId);
			IsPresentInSongPageDetails.Add(localizedTag.LocalizedStringId);
			return localizedTag.LocalizedStringId;
		}
	}
}

public class Tag
{
	public string TagName { get; set; } = "";
	public int LocId { get; set; }
	public string Category { get; set; } = "";
	public List<string> Synonyms { get; set; } = [];
}
