using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.AspNetCore.Mvc;

using System.Buffers;
using System.Collections.Immutable;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.recommendation.v3;

[ApiController]
[Route("recommendation/v3/search")]
public class Search(IMapService mapService, ITagService tagService) : ControllerBase
{
	private const uint VersionExactMatchScore = 100;
	private const uint TitleExactMatchScore = 10;
	private const uint TitlePartialMatchScore = 7;
	private const uint TitleFuzzyMatchScore = 5;
	private const uint ArtistExactMatchScore = 5;
	private const uint ArtistFuzzyMatchScore = 2;
	private const uint ArtistPresenceScore = 1;
	private const uint TagExactMatchScore = 6;
	private const uint TagPartialMatchScore = 4;
	private const uint TagFuzzyMatchScore = 2;

	readonly struct SongRelevance(KeyValuePair<Guid, JustDanceSongDBEntry> song, uint relevance)
	{
		public readonly KeyValuePair<Guid, JustDanceSongDBEntry> Song = song;
		public readonly uint Relevance = relevance;
	}

	[HttpPost(Name = "Search")]
	public IActionResult ProcessSearch([FromBody] SearchRequest searchRequest)
	{
		string searchInput = searchRequest.SearchInput ?? string.Empty;
		IReadOnlyDictionary<Guid, uint> tagScoreLookup = BuildTagScoreLookup(searchInput, tagService.TagDatabase.Tags);

		// Search the database
		SongRelevance[] searchSongResult = mapService.Songs
			.AsParallel()
			.Select(song => new SongRelevance(
                song,
				// Calculate relevance with higher score for exact artist match
				GetRelevance(searchInput, song.Value, tagScoreLookup)
            ))
			.Where(song => song.Relevance > 0)
			.OrderByDescending(song => song.Relevance)
			.ThenBy(song => song.Song.Value.Title)
			.ThenBy(song => song.Song.Value.MapName)
			.ToArray();

		List<Guid> mapIds = [.. searchSongResult.Select(song => song.Song.Key)];
		List<string> codeNames = [.. searchSongResult.Select(song => song.Song.Value.ParentMapName)];
		List<string> artistIds = [.. searchSongResult.Select(song => song.Song.Value.Artist).Distinct()];

		// Create the response
		SearchResponse searchResults = new()
		{
			Mapnames = mapIds,
			Maps = mapIds,
			Playlists = [],
			Artists = artistIds,
			Codenames = codeNames,
			Order = ["artists", "mapnames", "playlists"]
		};

		return Ok(searchResults);
	}

	static uint GetRelevance(string searchInput, JustDanceSongDBEntry song, IReadOnlyDictionary<Guid, uint> tagScoreLookup)
	{
		uint relevance = 0;

		string fullSongTitle = song.Title;
		if (!string.IsNullOrWhiteSpace(song.DanceVersionLocId.Name))
			fullSongTitle += " " + song.DanceVersionLocId.Name;

		bool partialMatchTitle = fullSongTitle.Contains(searchInput, StringComparison.OrdinalIgnoreCase);
		bool partialMatchArtist = song.Artist.Contains(searchInput, StringComparison.OrdinalIgnoreCase);

		if (uint.TryParse(searchInput, out uint searchNumber) && song.OriginalJDVersion == searchNumber)
			relevance += VersionExactMatchScore;

		// Title checks
		if (partialMatchTitle && searchInput.Length == fullSongTitle.Length)
			relevance += TitleExactMatchScore;
		else if (partialMatchTitle)
			relevance += TitlePartialMatchScore;
		else if (IsWithinAllowedEditDistance(searchInput, fullSongTitle) || IsPartialWordMatch(searchInput, fullSongTitle))
			relevance += TitleFuzzyMatchScore;

		// Artist checks
		if (partialMatchArtist && searchInput.Length == song.Artist.Length)
			relevance += ArtistExactMatchScore;
		else if (IsWithinAllowedEditDistance(searchInput, song.Artist) || IsPartialWordMatch(searchInput, song.Artist))
			relevance += ArtistFuzzyMatchScore;
		if (partialMatchArtist)
			relevance += ArtistPresenceScore;

		relevance += GetTagRelevance(song.TagIds, tagScoreLookup);

		return relevance;
	}

	static uint GetTagRelevance(ImmutableArray<GuidTag> tagIds, IReadOnlyDictionary<Guid, uint> tagScoreLookup)
	{
		if (tagIds.IsDefaultOrEmpty || tagScoreLookup.Count == 0)
			return 0;

		uint bestScore = 0;

		foreach (GuidTag guidTag in tagIds)
		{
			Tag? tag = guidTag.Tag;
			if (tag == null || tag.TagGuid == Guid.Empty)
				continue;

			if (tagScoreLookup.TryGetValue(tag.TagGuid, out uint score) && score > bestScore)
			{
				bestScore = score;

				if (bestScore == TagExactMatchScore)
					break;
			}
		}

		return bestScore;
	}

	static IReadOnlyDictionary<Guid, uint> BuildTagScoreLookup(string searchInput, IEnumerable<KeyValuePair<Guid, Tag>> availableTags)
	{
		if (string.IsNullOrWhiteSpace(searchInput))
			return ImmutableDictionary<Guid, uint>.Empty;

		Dictionary<Guid, uint> matchingTags = [];

		foreach (KeyValuePair<Guid, Tag> entry in availableTags)
		{
			uint bestScore = 0;

			foreach (string candidate in EnumerateTagTerms(entry.Value))
			{
				uint termScore = ScoreTagCandidate(searchInput, candidate);
				if (termScore > bestScore)
				{
					bestScore = termScore;

					if (bestScore == TagExactMatchScore)
						break;
				}
			}

			if (bestScore > 0)
				matchingTags[entry.Key] = bestScore;
		}

		return matchingTags.Count == 0
			? ImmutableDictionary<Guid, uint>.Empty
			: matchingTags;
	}

	static IEnumerable<string> EnumerateTagTerms(Tag tag)
	{
		if (!string.IsNullOrWhiteSpace(tag.LocId.Name))
			yield return tag.LocId.Name!;

		if (!string.IsNullOrWhiteSpace(tag.TagName) && !tag.TagName.Equals(tag.LocId.Name, StringComparison.OrdinalIgnoreCase))
			yield return tag.TagName;

		ImmutableArray<string> synonyms = tag.Synonyms;
		if (synonyms.IsDefaultOrEmpty)
			yield break;

		foreach (string synonym in synonyms)
		{
			if (!string.IsNullOrWhiteSpace(synonym))
				yield return synonym;
		}
	}

	static uint ScoreTagCandidate(string searchInput, string candidate)
	{
		if (string.IsNullOrWhiteSpace(candidate))
			return 0;

		if (candidate.Equals(searchInput, StringComparison.OrdinalIgnoreCase))
			return TagExactMatchScore;

		if (candidate.Contains(searchInput, StringComparison.OrdinalIgnoreCase))
			return TagPartialMatchScore;

		return (IsWithinAllowedEditDistance(searchInput, candidate) || IsPartialWordMatch(searchInput, candidate))
			? TagFuzzyMatchScore
			: 0;
	}

	static bool IsPartialWordMatch(string input, string title)
	{
		string[] words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return words.Any(word =>
			word.StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
			IsWithinAllowedEditDistance(input, word));
	}

	static bool IsWithinAllowedEditDistance(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
	{
		int maxLen = Math.Max(s1.Length, s2.Length);
		int maxAllowed = Math.Max(1, maxLen / 10); // at least 1, at most 10% of length
		int dist = LevenshteinDistance(s1, s2, maxAllowed);
		return dist >= 1 && dist <= maxAllowed;
	}

	/// <summary>
	/// Calculates the Levenshtein distance between two strings, which is a measure of the minimum number  of
	/// single-character edits (insertions, deletions, or substitutions) required to transform one string into the other.
	/// </summary>
	/// <param name="source">The first string to compare. Cannot be <see langword="null"/>.</param>
	/// <param name="target">The second string to compare. Cannot be <see langword="null"/>.</param>
	/// <param name="maxDistance">The maximum distance to calculate. If the distance exceeds this value, the method returns <see cref="int.MaxValue"/>.</param>
	/// <returns>The Levenshtein distance between the two strings. Returns 0 if both strings are identical.</returns>
	/// <exception cref="ArgumentNullException">Thrown if either <paramref name="source"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
	static int LevenshteinDistance(ReadOnlySpan<char> source, ReadOnlySpan<char> target, int maxDistance = int.MaxValue)
	{
		if (Math.Abs(source.Length - target.Length) > maxDistance)
			return int.MaxValue;

		int sourceLength = source.Length;
		int targetLength = target.Length;

		Span<int> previousRow = stackalloc int[targetLength + 1];
		Span<int> currentRow = stackalloc int[targetLength + 1];

		for (int j = 0; j <= targetLength; j++)
			previousRow[j] = j;

		for (int i = 1; i <= sourceLength; i++)
		{
			currentRow[0] = i;
			char sc = char.ToLowerInvariant(source[i - 1]);

			int minInRow = currentRow[0];

			for (int j = 1; j <= targetLength; j++)
			{
				char tc = char.ToLowerInvariant(target[j - 1]);
				int cost = (sc == tc) ? 0 : 1;

				currentRow[j] = Math.Min(
					Math.Min(currentRow[j - 1] + 1, previousRow[j] + 1),
					previousRow[j - 1] + cost
				);

				minInRow = Math.Min(minInRow, currentRow[j]);
			}

			if (minInRow > maxDistance)
				return int.MaxValue;

			// Swap prev and curr arrays for next iteration
			Span<int> temp = previousRow;
			previousRow = currentRow;
			currentRow = temp;

			// Clear the current row for the next iteration
			currentRow.Clear();
		}

		return previousRow[targetLength];
	}
}

public class SearchRequest
{
	public string SearchInput { get; set; } = "";
	public string Language { get; set; } = "";
}

public class SearchResponse
{
	public List<Guid> Mapnames { get; set; } = [];
	public List<Guid> Maps { get; set; } = [];
	public List<string> Playlists { get; set; } = [];
	public List<string> Artists { get; set; } = [];
	public List<string> Codenames { get; set; } = [];
	public List<string> Order { get; set; } = [];
}