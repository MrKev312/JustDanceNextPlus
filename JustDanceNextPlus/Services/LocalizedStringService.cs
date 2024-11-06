using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Services;

public class LocalizedStringService
{
	public Guid SpaceId { get; } = Guid.Parse("1da01a17-3bc7-4b5d-aedd-70a0915089b0");
	public List<LocalizedString> LocalizedStrings { get; } = [];
	readonly ConcurrentDictionary<string, int> localizedTags = new();
	readonly ConcurrentDictionary<Guid, int> localizedTagsGuid = new();

	public LocalizedString? GetLocalizedTag(string text)
	{
		if (localizedTags.TryGetValue(text, out int index))
			return LocalizedStrings[index];

		return null;
	}

	public LocalizedString? GetLocalizedTag(Guid id)
	{
		if (localizedTagsGuid.TryGetValue(id, out int index))
			return LocalizedStrings[index];

		return null;
	}

	public LocalizedString? GetLocalizedTag(int id)
	{
		if (id < LocalizedStrings.Count)
			return LocalizedStrings[id];

		return null;
	}

	public LocalizedString GetAddLocalizedTag(string text)
	{
		// If it contains the tag, return it
		LocalizedString? localizedTag = GetLocalizedTag(text);
		if (localizedTag != null)
			return localizedTag;

		// Lock the list
		lock (LocalizedStrings)
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

			int index = LocalizedStrings.Count;

			localizedTag = new()
			{
				OasisIdInt = index + 10000,
				LocaleCode = "en-US",
				DisplayString = text,
				LocalizedStringId = guid
			};

			LocalizedStrings.Add(localizedTag);
			localizedTags.TryAdd(text, index);
			localizedTagsGuid.TryAdd(guid, index);
			return localizedTag;
		}
	}
}

public class LocalizedString
{
	[JsonIgnore]
	public int OasisIdInt { get; set; }
	public string OasisId => OasisIdInt.ToString();
	public string LocaleCode { get; set; } = "";
	public string DisplayString { get; set; } = "";
	public Guid LocalizedStringId { get; set; }
	public object? Obj { get; set; }
	public int SpaceRevision { get; set; } = 121;
}