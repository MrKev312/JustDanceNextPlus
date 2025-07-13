using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

using System.Buffers;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.recommendation.v3;

[ApiController]
[Route("recommendation/v3/search")]
public class Search(MapService mapService) : ControllerBase
{
	readonly struct SongRelevance(KeyValuePair<Guid, JustDanceSongDBEntry> song, uint relevance)
	{
		public readonly KeyValuePair<Guid, JustDanceSongDBEntry> Song = song;
		public readonly uint Relevance = relevance;
	}

	[HttpPost(Name = "Search")]
	public IActionResult ProcessSearch([FromBody] SearchRequest searchRequest)
	{
		// Search the database
		var searchSongResult = mapService.SongDB.Songs
			.AsParallel()
			.Select(song => new SongRelevance(
                song,
				// Calculate relevance with higher score for exact artist match
				GetRelevance(searchRequest.SearchInput, song.Value)
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

	static uint GetRelevance(string searchInput, JustDanceSongDBEntry song)
	{
		uint relevance = 0;

		string fullSongTitle = song.Title;
		if (!string.IsNullOrWhiteSpace(song.DanceVersionLocId.Name))
			fullSongTitle += " " + song.DanceVersionLocId.Name;

		bool partialMatchTitle = fullSongTitle.Contains(searchInput, StringComparison.OrdinalIgnoreCase);
		bool partialMatchArtist = song.Artist.Contains(searchInput, StringComparison.OrdinalIgnoreCase);

		if (uint.TryParse(searchInput, out uint searchNumber) && song.OriginalJDVersion == searchNumber)
			relevance += 100;

		// Title checks
		if (partialMatchTitle && searchInput.Length == fullSongTitle.Length)
			relevance += 10;
		else if (partialMatchTitle)
			relevance += 7;
		else if (IsWithinAllowedEditDistance(searchInput, fullSongTitle) || IsPartialWordMatch(searchInput, fullSongTitle))
			relevance += 5;

		// Artist checks
		if (partialMatchArtist && searchInput.Length == song.Artist.Length)
			relevance += 5;
		else if (IsWithinAllowedEditDistance(searchInput, song.Artist) || IsPartialWordMatch(searchInput, song.Artist))
			relevance += 2;
		if (partialMatchArtist)
			relevance += 1;

		return relevance;
	}

	static bool IsPartialWordMatch(string input, string title)
	{
		var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
	/// <param name="s">The first string to compare. Cannot be <see langword="null"/>.</param>
	/// <param name="t">The second string to compare. Cannot be <see langword="null"/>.</param>
	/// <param name="maxDistance">The maximum distance to calculate. If the distance exceeds this value, the method returns <see cref="int.MaxValue"/>.</param>
	/// <returns>The Levenshtein distance between the two strings. Returns 0 if both strings are identical.</returns>
	/// <exception cref="ArgumentNullException">Thrown if either <paramref name="s"/> or <paramref name="t"/> is <see langword="null"/>.</exception>
	static int LevenshteinDistance(ReadOnlySpan<char> s, ReadOnlySpan<char> t, int maxDistance = int.MaxValue)
	{
		if (Math.Abs(s.Length - t.Length) > maxDistance)
			return int.MaxValue;

		int n = s.Length;
		int m = t.Length;

		Span<int> prev = stackalloc int[m + 1];
		Span<int> curr = stackalloc int[m + 1];

		for (int j = 0; j <= m; j++)
			prev[j] = j;

		for (int i = 1; i <= n; i++)
		{
			curr[0] = i;
			char sc = char.ToLowerInvariant(s[i - 1]);

			int minInRow = curr[0];

			for (int j = 1; j <= m; j++)
			{
				char tc = char.ToLowerInvariant(t[j - 1]);
				int cost = (sc == tc) ? 0 : 1;

				curr[j] = Math.Min(
					Math.Min(curr[j - 1] + 1, prev[j] + 1),
					prev[j - 1] + cost
				);

				minInRow = Math.Min(minInRow, curr[j]);
			}

			if (minInRow > maxDistance)
				return int.MaxValue;

			// Swap prev and curr arrays for next iteration
			Span<int> temp = prev;
			prev = curr;
			curr = temp;

			// Clear the current row for the next iteration
			curr.Clear();
		}

		return prev[m];
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