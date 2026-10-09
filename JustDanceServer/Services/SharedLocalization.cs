using JustDanceServer.Utilities;

using System.Globalization;
using System.Text.Json;

namespace JustDanceServer.Services;

public sealed record SharedTextAlias
{
	public required string Namespace { get; init; }
	public required int Id { get; init; }
	public Guid? Guid { get; init; }
}

public sealed record SharedTextDefinition
{
	public required string Key { get; init; }
	public required Dictionary<string, string> Translations { get; init; }
	public SharedTextAlias[] Aliases { get; init; } = [];
}

public partial class LocalizedStringService
{
	public LocalizedStringDatabase GetDatabase(string? locale)
	{
		lock (stringLock)
		{
			return new()
			{
				LocalizedStrings = [.. Database.LocalizedStrings.GroupBy(value => value.OasisId)
					.Select(group => group.First() with
					{
						LocaleCode = LocaleFallback.Normalize(locale),
						DisplayString = jdNextText.TryGetValue(group.Key, out SharedTextDefinition? shared)
							? LocaleFallback.Resolve(shared.Translations, locale, group.First().DisplayString)
							: LocaleFallback.Resolve(group.GroupBy(value => value.LocaleCode, StringComparer.OrdinalIgnoreCase)
								.ToDictionary(values => values.Key, values => values.First().DisplayString), locale, group.First().DisplayString)
					}).OrderBy(value => value.OasisId)]
			};
		}
	}

	private IReadOnlyDictionary<string, SharedTextDefinition> sharedText = new Dictionary<string, SharedTextDefinition>();
	private IReadOnlyDictionary<string, LocalizedString> sharedTags = new Dictionary<string, LocalizedString>();
	private IReadOnlyDictionary<int, SharedTextDefinition> jdNextText = new Dictionary<int, SharedTextDefinition>();

	static LocalizedStringService()
	{
		for (int index = 0; index < defaultLocalizedStrings.Count; index++)
		{
			LocalizedString value = defaultLocalizedStrings[index];
			defaultLocalizedStrings[index] = value with
			{
				LocalizedStringId = StableContentId.Create($"jdnext-oasis:{value.OasisId.ToString(CultureInfo.InvariantCulture)}")
			};
		}
	}

	public string ResolveText(string key, string? locale, string? fallback = null)
		=> sharedText.TryGetValue(key, out SharedTextDefinition? text)
			? LocaleFallback.Resolve(text.Translations, locale, fallback ?? string.Empty)
			: fallback ?? throw new KeyNotFoundException($"Shared text '{key}' is not defined.");

	public LocalizedString? GetTextTag(string key)
		=> sharedTags.GetValueOrDefault(key);

	public string ResolveTag(LocalizedString tag, string? locale)
	{
		lock (stringLock)
		{
			if (jdNextText.TryGetValue(tag.OasisId, out SharedTextDefinition? shared))
				return LocaleFallback.Resolve(shared.Translations, locale, tag.DisplayString);
			foreach (string candidate in LocaleFallback.GetCandidates(locale))
			{
				LocalizedString? translation = Database.LocalizedStrings.FirstOrDefault(value => value.OasisId == tag.OasisId
					&& string.Equals(value.LocaleCode, candidate, StringComparison.OrdinalIgnoreCase));
				if (translation is not null)
					return translation.DisplayString;
			}

			return tag.DisplayString;
		}
	}

	public int? GetJduAlias(int jdNextId)
		=> jdNextText.TryGetValue(jdNextId, out SharedTextDefinition? shared)
			? shared.Aliases.FirstOrDefault(alias => alias.Namespace == "jdu")?.Id : null;

	public IReadOnlyDictionary<string, Dictionary<string, string>> GetJduLocalisation()
	{
		Dictionary<string, Dictionary<string, string>> result = [with(StringComparer.Ordinal)];
		foreach (SharedTextDefinition text in sharedText.Values)
		{
			foreach (SharedTextAlias alias in text.Aliases.Where(value => value.Namespace == "jdu"))
			{
				// The retail client lowercases both language and region before indexing JSON.
				Dictionary<string, string> translations = text.Translations.ToDictionary(
					value => value.Key.ToLowerInvariant(), value => value.Value, StringComparer.OrdinalIgnoreCase);
				// The client only tries exact and neutral language keys, not English.
				foreach (string locale in new[] { "en", "fr", "de", "es", "it", "nl", "pt", "ru", "ja", "ko", "zh", "pl", "ar", "tr", "sv", "da", "nb", "fi", "cs", "hu" })
					translations.TryAdd(locale, LocaleFallback.Resolve(text.Translations, locale, string.Empty));
				result.Add(alias.Id.ToString(CultureInfo.InvariantCulture), translations);
			}
		}

		return result;
	}

	private async Task LoadSharedTextAsync(string jsonPath, JsonSettingsService jsonSettings)
	{
		Dictionary<string, SharedTextDefinition> definitions = CreateDefaultSharedText()
			.ToDictionary(value => value.Key, StringComparer.Ordinal);
		string path = Path.Combine(jsonPath, "shared-text.json");
		HashSet<string> authoredKeys = [with(StringComparer.Ordinal)];
		if (fileSystem.FileExists(path))
		{
			using Stream stream = fileSystem.OpenRead(path);
			SharedTextDefinition[] authored = await JsonSerializer.DeserializeAsync<SharedTextDefinition[]>(stream, jsonSettings.PrettyPascalFormat)
				?? throw new InvalidOperationException("Shared text must be an array of definitions.");
			foreach (SharedTextDefinition text in authored)
			{
				if (text is null || string.IsNullOrWhiteSpace(text.Key) || !authoredKeys.Add(text.Key))
					throw new InvalidOperationException("Invalid or duplicate shared text key.");
				definitions[text.Key] = text;
			}
		}

		HashSet<(string, int)> aliases = [];
		Dictionary<string, LocalizedString> tags = [with(StringComparer.Ordinal)];
		List<LocalizedString> additional = [];
		foreach (SharedTextDefinition text in definitions.Values.OrderBy(value => value.Key, StringComparer.Ordinal))
		{
			if (text.Aliases is null || text.Translations is null || !text.Translations.TryGetValue("en-US", out string? englishTranslation)
				|| text.Translations.Any(value => string.IsNullOrWhiteSpace(value.Key) || value.Value is null)
				|| text.Translations.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != text.Translations.Count)
				throw new InvalidOperationException($"Shared text '{text.Key}' needs an en-US fallback and valid translations.");
			foreach (SharedTextAlias alias in text.Aliases)
			{
				if (alias is null || alias.Id < 0 || alias.Namespace is not ("jdnext" or "jdu") || !aliases.Add((alias.Namespace, alias.Id)))
					throw new InvalidOperationException($"Invalid or duplicate shared text alias for '{text.Key}'.");
				if (alias.Namespace != "jdnext")
					continue;
				LocalizedString? existing = GetLocalizedTag(alias.Id);
				if (existing is not null && !authoredKeys.Contains(text.Key) && existing.DisplayString != englishTranslation)
					throw new InvalidOperationException($"Default text '{text.Key}' conflicts with existing ID {alias.Id}; author an explicit alias in shared-text.json.");
				if (alias.Guid == Guid.Empty || (existing is not null && alias.Guid.HasValue && alias.Guid != existing.LocalizedStringId))
					throw new InvalidOperationException($"Shared text '{text.Key}' must preserve its existing GUID.");
				Guid id = existing?.LocalizedStringId ?? alias.Guid ?? StableContentId.Create($"shared-text:{text.Key}:{alias.Id.ToString(CultureInfo.InvariantCulture)}");
				if (Database.LocalizedStrings.Concat(defaultLocalizedStrings).Concat(additional)
					.Any(value => value.LocalizedStringId == id && value.OasisId != alias.Id))
					throw new InvalidOperationException($"Shared text '{text.Key}' reuses a GUID assigned to another ID.");
				foreach ((string locale, string translation) in text.Translations)
				{
					additional.Add(new LocalizedString
					{
						OasisId = alias.Id, LocalizedStringId = id, LocaleCode = locale, DisplayString = translation
					});
				}

				tags[text.Key] = new LocalizedString
				{
					OasisId = alias.Id, LocalizedStringId = id, LocaleCode = "en-US", DisplayString = englishTranslation
				};
			}
		}

		HashSet<(int, string)> replaced = [.. additional.Select(value => (value.OasisId, value.LocaleCode.ToLowerInvariant()))];
		Database.LocalizedStrings = [.. Database.LocalizedStrings
			.Where(value => !replaced.Contains((value.OasisId, value.LocaleCode.ToLowerInvariant())))
			.Concat(additional).OrderBy(value => value.OasisId).ThenBy(value => value.LocaleCode, StringComparer.Ordinal)];
		sharedText = definitions;
		sharedTags = tags;
		jdNextText = definitions.Values.SelectMany(value => value.Aliases
			.Where(alias => alias.Namespace == "jdnext").Select(alias => (alias.Id, Text: value)))
			.ToDictionary(value => value.Id, value => value.Text);
	}

	private static IEnumerable<SharedTextDefinition> CreateDefaultSharedText()
	{
		(string Key, string Title, int Id)[] defaults =
		[
			("discovery.for-you", "For You", 900001),
			("discovery.discover", "Discover", 900002),
			("discovery.top-replayed", "Your Most Played", 900003),
			("discovery.top-country", "Popular in Your Country", 900004),
			("discovery.recent", "Recently Played", 900005),
			("discovery.popular", "Popular Songs", 900006),
			("discovery.new", "Newly Added Songs!", 900007)
		];
		return defaults.Select(value => new SharedTextDefinition
		{
			Key = value.Key,
			Translations = new Dictionary<string, string> { ["en-US"] = value.Title },
			Aliases = [new SharedTextAlias { Namespace = "jdnext", Id = value.Id }]
		});
	}
}
