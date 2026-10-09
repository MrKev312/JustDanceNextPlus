using JustDanceServer.Configuration;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Options;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Services;

[JsonConverter(typeof(JsonStringEnumConverter<MenuRowSource>))]
public enum MenuRowSource
{
	Recommendation,
	Maps,
	Playlist,
	Tag,
	Alphabetical,
	OriginalGame,
	RecentlyAdded
}

public sealed record MenuRowDefinition
{
	public required string Key { get; init; }
	public required string TitleKey { get; init; }
	public MenuRowSource Source { get; init; }
	[JsonConverter(typeof(JsonStringEnumConverter<RecommendationPolicy>))]
	public RecommendationPolicy Policy { get; init; } = RecommendationPolicy.Popular;
	public string[] Maps { get; init; } = [];
	public Guid? PlaylistId { get; init; }
	public string? Tag { get; init; }
	public int? GameYear { get; init; }
	public int Count { get; init; } = 20;
	public string CoverUrl { get; init; } = string.Empty;
	public DateTimeOffset? StartsAt { get; init; }
	public DateTimeOffset? EndsAt { get; init; }
}

public sealed record MenuDefinition
{
	public required string Key { get; init; }
	public required string[] Surfaces { get; init; }
	public required MenuRowDefinition[] Rows { get; init; }
}

public sealed record MenuRow(Guid Id, string Key, string Title, LocalizedString? TitleTag, string[] MapNames, string CoverUrl);

public interface IMenuDefinitionService : ILoadService
{
	IReadOnlyList<MenuRowDefinition> GetRows(string surface);
}

public sealed class MenuDefinitionService(
	IOptions<PathSettings> paths,
	IFileSystem fileSystem,
	JsonSettingsService jsonSettings,
	IMapService maps,
	IPlaylistService playlists,
	ILocalizedStringService text,
	IActivityPageService activityPages) : IMenuDefinitionService
{
	private IReadOnlyList<MenuDefinition> definitions = [];
	private static readonly string[] Surfaces = ["jdnext-activity", "jdu-classic-party", "jdu-home", "jdu-playlists"];

	public async Task LoadData()
	{
		string path = Path.Combine(paths.Value.JsonsPath, "menus.json");
		MenuDefinition[] loaded;
		if (fileSystem.FileExists(path))
		{
			using Stream stream = fileSystem.OpenRead(path);
			loaded = await JsonSerializer.DeserializeAsync<MenuDefinition[]>(stream, jsonSettings.PrettyPascalFormat)
				?? throw new InvalidOperationException("Menus must be an array of definitions.");
		}
		else
		{
			loaded = [new MenuDefinition
			{
				Key = "discovery", Surfaces = Surfaces,
				Rows =
				[
					new() { Key = "reco-for_you", TitleKey = "discovery.for-you", Policy = RecommendationPolicy.ForYou },
					new() { Key = "reco-discover", TitleKey = "discovery.discover", Policy = RecommendationPolicy.Discover },
					new() { Key = "reco-top_replayed", TitleKey = "discovery.top-replayed", Policy = RecommendationPolicy.TopReplayed },
					new() { Key = "reco-top_country", TitleKey = "discovery.top-country", Policy = RecommendationPolicy.TopCountry }
				]
			}];
		}

		HashSet<string> keys = [with(StringComparer.Ordinal)];
		HashSet<string> rowKeys = [with(StringComparer.OrdinalIgnoreCase)];
		foreach (MenuDefinition definition in loaded)
		{
			if (definition is null)
				throw new InvalidOperationException("Menu definitions cannot contain null entries.");
			if (string.IsNullOrWhiteSpace(definition.Key) || !keys.Add(definition.Key)
				|| definition.Surfaces is null || definition.Rows is null
				|| definition.Surfaces.Length == 0 || definition.Surfaces.Any(surface => !Surfaces.Contains(surface, StringComparer.Ordinal)))
				throw new InvalidOperationException($"Invalid menu definition '{definition.Key}'.");
			foreach (MenuRowDefinition row in definition.Rows)
			{
				if (row is null || row.Maps is null || string.IsNullOrWhiteSpace(row.TitleKey))
					throw new InvalidOperationException($"Menu '{definition.Key}' contains an incomplete row.");
				if (string.IsNullOrWhiteSpace(row.Key) || !rowKeys.Add(row.Key) || row.Count is < 1 or > 100
					|| !Enum.IsDefined(row.Source) || !Enum.IsDefined(row.Policy)
					|| (row.StartsAt.HasValue && row.EndsAt <= row.StartsAt))
					throw new InvalidOperationException($"Invalid or duplicate menu row '{row.Key}'.");
				_ = text.ResolveText(row.TitleKey, "en-US");
				if (definition.Surfaces.Contains("jdu-playlists") && Guid.TryParse(row.Key, out Guid playlistId)
					&& playlists.Playlists.ContainsKey(playlistId))
					throw new InvalidOperationException($"Menu row '{row.Key}' conflicts with a curated playlist.");
				if (definition.Surfaces.Contains("jdnext-activity") && text.GetTextTag(row.TitleKey) is null)
					throw new InvalidOperationException($"Menu row '{row.Key}' requires a JDNext text alias.");
				if (definition.Surfaces.Contains("jdnext-activity")
					&& activityPages.ActivityPage.Categories.ContainsKey(StableContentId.Create($"menu:{row.Key}")))
					throw new InvalidOperationException($"Menu row '{row.Key}' conflicts with an existing activity category.");
				if (row.Source == MenuRowSource.Maps && (row.Maps.Length == 0 || row.Maps.Any(name => maps.GetMap(name) is null)))
					throw new InvalidOperationException($"Menu row '{row.Key}' references missing maps.");
				if (row.Source == MenuRowSource.Playlist && (!row.PlaylistId.HasValue || !playlists.Playlists.ContainsKey(row.PlaylistId.Value)))
					throw new InvalidOperationException($"Menu row '{row.Key}' references a missing playlist.");
				if ((row.Source == MenuRowSource.Tag && string.IsNullOrWhiteSpace(row.Tag))
					|| (row.Source == MenuRowSource.OriginalGame && row.GameYear is null)
					|| (row.Policy == RecommendationPolicy.Related && row.Source == MenuRowSource.Recommendation))
					throw new InvalidOperationException($"Menu row '{row.Key}' has an incomplete selector.");
			}
		}

		definitions = loaded;
	}

	public IReadOnlyList<MenuRowDefinition> GetRows(string surface)
		=> [.. definitions.Where(value => value.Surfaces.Contains(surface, StringComparer.Ordinal)).SelectMany(value => value.Rows)];
}

public interface IMenuService
{
	ValueTask<IReadOnlyList<MenuRow>> GetRowsAsync(string surface, PlayerRequestContext context);
}

public sealed class MenuService(
	IMenuDefinitionService definitions,
	IRecommendationService recommendations,
	IMapService maps,
	IPlaylistService playlists,
	ILocalizedStringService text,
	TimeProvider timeProvider) : IMenuService
{
	public async ValueTask<IReadOnlyList<MenuRow>> GetRowsAsync(string surface, PlayerRequestContext context)
	{
		List<MenuRow> rows = [];
		DateTimeOffset now = timeProvider.GetUtcNow();
		foreach (MenuRowDefinition definition in definitions.GetRows(surface))
		{
			if (definition.StartsAt > now || definition.EndsAt <= now)
				continue;
			IEnumerable<string> selected = definition.Source switch
			{
				MenuRowSource.Recommendation => await recommendations.GetMapsAsync(
					new RecommendationQuery(definition.Policy, context.ProfileId, definition.Count)),
				MenuRowSource.Maps => definition.Maps,
				MenuRowSource.Playlist => playlists.Playlists[definition.PlaylistId!.Value].MapNames,
				MenuRowSource.Tag => maps.Maps.Values.Where(map => map.Song.Tags.Contains(definition.Tag!, StringComparer.OrdinalIgnoreCase))
					.Select(map => map.Song.MapName).Order(StringComparer.OrdinalIgnoreCase),
				MenuRowSource.Alphabetical => maps.Maps.Values.OrderBy(map => map.Song.Title, StringComparer.OrdinalIgnoreCase)
					.ThenBy(map => map.Song.MapName, StringComparer.OrdinalIgnoreCase).Select(map => map.Song.MapName),
				MenuRowSource.OriginalGame => maps.Maps.Values.Where(map => map.Song.OriginalJDVersion == definition.GameYear)
					.Select(map => map.Song.MapName).Order(StringComparer.OrdinalIgnoreCase),
				MenuRowSource.RecentlyAdded => maps.RecentlyAdded.Select(tag => maps.Maps[tag.Guid].Song.MapName),
				_ => throw new InvalidOperationException($"Unknown selector '{definition.Source}'.")
			};
			string[] names = [.. selected.Select(name => maps.GetMap(name)
					?? throw new InvalidOperationException($"Selected map '{name}' is not in the catalog."))
				.Where(map => context.GameYear is not (>= 2016 and <= 2018) || JduCatalogSelection.IsClassicCompatible(map.Song))
				.Select(map => map.Song.MapName).Distinct(StringComparer.OrdinalIgnoreCase).Take(definition.Count)];
			if (names.Length == 0)
				continue;
			rows.Add(new MenuRow(StableContentId.Create($"menu:{definition.Key}"), definition.Key,
				text.ResolveText(definition.TitleKey, context.Locale), text.GetTextTag(definition.TitleKey), names, definition.CoverUrl));
		}

		return rows;
	}
}
