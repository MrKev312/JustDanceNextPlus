using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

using System.Globalization;

namespace JustDanceServer.Services;

public enum RecommendationPolicy
{
	Popular,
	Related,
	ForYou,
	Discover,
	Recent,
	Favorites,
	TopReplayed,
	TopCountry
}

public sealed record RecommendationQuery(
	RecommendationPolicy Policy,
	Guid? ProfileId = null,
	int? Count = 20,
	string? RelatedMap = null);

public interface IRecommendationService
{
	ValueTask<string[]> GetMapsAsync(RecommendationQuery query);
	ValueTask<IReadOnlyDictionary<string, string[]>> GetRelatedSongsAsync();
}

public sealed class RecommendationService(
	UserDataContext dbContext,
	IMapService mapService,
	TimeProvider timeProvider) : IRecommendationService
{
	public async ValueTask<string[]> GetMapsAsync(RecommendationQuery query)
	{
		JustDanceSongDBEntry[] catalog = GetCatalog();
		Dictionary<string, JustDanceSongDBEntry> byName = catalog.ToDictionary(song => song.MapName, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, long> popularity = await GetPopularityAsync();
		string[] popular = OrderByPopularity(catalog, popularity);
		int count = Math.Clamp(query.Count ?? catalog.Length, 0, catalog.Length);
		if (count == 0)
			return [];
		if (query.Policy == RecommendationPolicy.Popular)
			return [.. popular.Take(count)];
		if (query.Policy == RecommendationPolicy.Related)
			return byName.TryGetValue(query.RelatedMap ?? string.Empty, out JustDanceSongDBEntry? source)
				? OrderRelated(catalog, source, popularity).Take(count).ToArray() : [];

		Guid? player = query.ProfileId;
		List<MapStats> scores = player.HasValue
			? await dbContext.MapScores.AsNoTracking().Where(value => value.ProfileId == player).ToListAsync() : [];
		string[] favorites = player.HasValue
			? await dbContext.PlayerMapFavorites.AsNoTracking().Where(value => value.ProfileId == player)
				.OrderByDescending(value => value.FavoritedAtUtc).ThenBy(value => value.MapName)
				.Select(value => value.MapName).ToArrayAsync() : [];
		DateTime cutoff = timeProvider.GetUtcNow().UtcDateTime - PlayerActivityService.Retention;
		string[] recent = player.HasValue
			? await dbContext.PlayerMapPlays.AsNoTracking()
				.Where(value => value.ProfileId == player && value.PlayedAtUtc >= cutoff)
				.GroupBy(value => value.MapName)
				.OrderByDescending(group => group.Max(value => value.PlayedAtUtc)).ThenBy(group => group.Key)
				.Select(group => group.Key).ToArrayAsync() : [];
		// Compare ordinals only within their source scope, never across independent saves.
		List<PlayerMapOrdinal> ordinals = player.HasValue
			? await dbContext.PlayerMapOrdinals.AsNoTracking()
				.Where(value => value.ProfileId == player && value.ObservedAtUtc >= cutoff).ToListAsync() : [];
		string[] legacy = [.. ordinals.GroupBy(value => value.SourceScope).OrderBy(group => group.Key, StringComparer.Ordinal)
			.SelectMany(group => group.OrderByDescending(value => value.Ordinal).ThenBy(value => value.MapName, StringComparer.OrdinalIgnoreCase))
			.Select(value => value.MapName).Distinct(StringComparer.OrdinalIgnoreCase)];
		HashSet<string> known = new(scores.Select(value => value.MapName).Concat(recent).Concat(legacy), StringComparer.OrdinalIgnoreCase);
		IEnumerable<string> result;
		switch (query.Policy)
		{
			case RecommendationPolicy.Favorites:
				result = favorites;
				break;
			case RecommendationPolicy.Recent:
				result = recent;
				break;
			case RecommendationPolicy.TopReplayed:
				result = scores.Where(value => value.PlayCount > 0).OrderByDescending(value => value.PlayCount).ThenBy(value => value.MapName, StringComparer.OrdinalIgnoreCase)
					.Select(value => value.MapName).Concat(popular);
				break;
			case RecommendationPolicy.TopCountry:
				int? country = player.HasValue ? await dbContext.PlayerMetadata.AsNoTracking()
					.Where(value => value.ProfileId == player).Select(value => (int?)value.CountryId).FirstOrDefaultAsync() : null;
				Dictionary<string, long> countryPlays = country is > 0
					? await (from score in dbContext.MapScores.AsNoTracking()
						join metadata in dbContext.PlayerMetadata.AsNoTracking() on score.ProfileId equals metadata.ProfileId
						where metadata.CountryId == country
						group score by score.MapName into grouped
						select new { MapName = grouped.Key, Plays = grouped.Sum(value => (long)value.PlayCount) })
						.ToDictionaryAsync(value => value.MapName, value => value.Plays, StringComparer.OrdinalIgnoreCase)
					: popularity;
				result = catalog.OrderByDescending(song => countryPlays.GetValueOrDefault(song.MapName))
					.ThenByDescending(song => popularity.GetValueOrDefault(song.MapName))
					.ThenBy(song => song.Title, StringComparer.OrdinalIgnoreCase)
					.ThenBy(song => song.MapName, StringComparer.OrdinalIgnoreCase).Select(song => song.MapName);
				break;
			case RecommendationPolicy.Discover:
				result = DailyOrder(popular.Where(name => !known.Contains(name)), query, catalog).Concat(popular);
				break;
			case RecommendationPolicy.ForYou:
				List<string> selected = [];
				HashSet<string> selectedMaps = [with(StringComparer.OrdinalIgnoreCase)];
				HashSet<string> selectedParents = [with(StringComparer.OrdinalIgnoreCase)];
				void Add(IEnumerable<string> candidates, int quota)
				{
					int added = 0;
					foreach (string name in candidates)
					{
						if (added >= quota || selected.Count >= count)
							break;
						if (!byName.TryGetValue(name, out JustDanceSongDBEntry? song) || selectedMaps.Contains(name))
							continue;
						string parent = string.IsNullOrWhiteSpace(song.ParentMapName) ? name : song.ParentMapName;
						if (!selectedParents.Add(parent))
							continue;
						selectedMaps.Add(name);
						selected.Add(name);
						added++;
					}
				}

				string[] signals = [.. favorites.Concat(recent).Concat(legacy)
					.Distinct(StringComparer.OrdinalIgnoreCase).Where(byName.ContainsKey).Take(12)];
				Add((mapService.RecentlyAdded ?? []).Select(value => mapService.Maps.TryGetValue(value.Guid, out ServerMap? map)
					? map.Song.MapName : string.Empty), Math.Min(4, Math.Max(1, count / 5)));
				IEnumerable<string> related = catalog.Where(song => signals.Any(name => GetRelationship(song, byName[name]) < 2))
					.Where(song => !signals.Contains(song.MapName, StringComparer.OrdinalIgnoreCase))
					.Select(song => song.MapName);
				Add(DailyOrder(related, query, catalog).OrderBy(name => signals.Min(signal => GetRelationship(byName[name], byName[signal]))),
					Math.Min(8, Math.Max(1, count * 2 / 5)));
				Add(recent.Reverse(), Math.Min(4, Math.Max(1, count / 5)));
				Add(DailyOrder(popular.Where(name => !known.Contains(name)), query, catalog).Concat(popular), count);
				result = selected;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(query), query.Policy, "Unknown recommendation policy.");
		}

		return [.. result.Where(byName.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase).Take(count)];
	}

	public async ValueTask<IReadOnlyDictionary<string, string[]>> GetRelatedSongsAsync()
	{
		JustDanceSongDBEntry[] catalog = GetCatalog();
		Dictionary<string, long> popularity = await GetPopularityAsync();
		return catalog.ToDictionary(song => song.MapName,
			song => OrderRelated(catalog, song, popularity).Take(10).ToArray(), StringComparer.OrdinalIgnoreCase);
	}

	private JustDanceSongDBEntry[] GetCatalog()
		=> [.. mapService.Maps.Values.Select(map => map.Song)
			.Where(song => !string.IsNullOrWhiteSpace(song.MapName))
			.DistinctBy(song => song.MapName, StringComparer.OrdinalIgnoreCase)
			.OrderBy(song => song.MapName, StringComparer.OrdinalIgnoreCase)];

	private async ValueTask<Dictionary<string, long>> GetPopularityAsync()
		=> await dbContext.MapScores.AsNoTracking().GroupBy(value => value.MapName)
			.Select(group => new { MapName = group.Key, Plays = group.Sum(value => (long)value.PlayCount) })
			.ToDictionaryAsync(value => value.MapName, value => value.Plays, StringComparer.OrdinalIgnoreCase);

	private static string[] OrderByPopularity(IEnumerable<JustDanceSongDBEntry> songs, IReadOnlyDictionary<string, long> popularity)
		=> [.. songs.OrderByDescending(song => popularity.GetValueOrDefault(song.MapName))
			.ThenBy(song => song.Title, StringComparer.OrdinalIgnoreCase)
			.ThenBy(song => song.MapName, StringComparer.OrdinalIgnoreCase).Select(song => song.MapName)];

	private static IEnumerable<string> OrderRelated(IEnumerable<JustDanceSongDBEntry> catalog, JustDanceSongDBEntry source,
		IReadOnlyDictionary<string, long> popularity)
		=> catalog.Where(song => !string.Equals(song.MapName, source.MapName, StringComparison.OrdinalIgnoreCase))
			.OrderBy(song => GetRelationship(song, source)).ThenByDescending(song => popularity.GetValueOrDefault(song.MapName))
			.ThenBy(song => song.Title, StringComparer.OrdinalIgnoreCase).ThenBy(song => song.MapName, StringComparer.OrdinalIgnoreCase)
			.Select(song => song.MapName);

	private static int GetRelationship(JustDanceSongDBEntry candidate, JustDanceSongDBEntry source)
	{
		if (!string.IsNullOrWhiteSpace(source.Artist) && string.Equals(candidate.Artist, source.Artist, StringComparison.OrdinalIgnoreCase))
			return 0;
		if ((source.OriginalJDVersion > 0 && candidate.OriginalJDVersion == source.OriginalJDVersion)
			|| candidate.Tags.Intersect(source.Tags, StringComparer.OrdinalIgnoreCase).Any())
			return 1;
		return 2;
	}

	private IEnumerable<string> DailyOrder(IEnumerable<string> maps, RecommendationQuery query, JustDanceSongDBEntry[] catalog)
	{
		string revision = StableContentId.Hash(string.Join('\n', catalog.Select(song => song.MapName)));
		string seed = $"v1|{query.ProfileId?.ToString("N") ?? "anonymous"}|{query.Policy}|{timeProvider.GetUtcNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}|{revision}";
		return maps.OrderBy(name => StableContentId.Hash($"{seed}|{name}"), StringComparer.Ordinal)
			.ThenBy(name => name, StringComparer.OrdinalIgnoreCase);
	}
}
