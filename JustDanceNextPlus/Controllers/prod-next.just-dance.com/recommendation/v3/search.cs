using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.recommendation.v3;

[ApiController]
[Route("recommendation/v3/search")]
public class Search(MapService mapService) : ControllerBase
{
	[HttpPost(Name = "Search")]
	public IActionResult ProcessSearch([FromBody] SearchRequest searchRequest)
	{
		// Normalize search input for comparison
		string normalizedSearchInput = searchRequest.SearchInput.ToLower();

		// Search the database
		var searchSongResult = mapService.SongDB.Songs
			.AsParallel()
			.Select(song => new
			{
				Song = song,
				// Calculate relevance with higher score for exact artist match
				Relevance = GetRelevance(normalizedSearchInput, song.Value)
			})
			.Where(song => song.Relevance > 0)
			.OrderByDescending(song => song.Relevance)
			.ThenBy(song => song.Song.Value.Title)
			.ThenBy(song => song.Song.Value.MapName)
			.ToArray();

		Guid[] mapIds = [.. searchSongResult.Select(song => song.Song.Key)];
		string[] codeNames = [.. searchSongResult.Select(song => song.Song.Value.ParentMapName)];
		string[] artistIds = [.. searchSongResult.Select(song => song.Song.Value.Artist).Distinct()];

		// Create the response
		SearchResponse searchResults = new()
		{
			Mapnames = [.. mapIds],
			Maps = [.. mapIds],
			Playlists = [],
			Artists = [.. artistIds],
			Codenames = [.. codeNames],
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

	static bool IsWithinAllowedEditDistance(string s1, string s2)
	{
		int dist = LevenshteinDistance(s1.ToLowerInvariant(), s2.ToLowerInvariant());
		int maxLen = Math.Max(s1.Length, s2.Length);
		int maxAllowed = Math.Max(1, maxLen / 10); // at least 1, at most 10% of length
		return dist >= 1 && dist <= maxAllowed;
	}

	static int LevenshteinDistance(string s, string t)
	{
		int n = s.Length;
		int m = t.Length;
		int[,] d = new int[n + 1, m + 1];

		for (int i = 0; i <= n; i++) d[i, 0] = i;
		for (int j = 0; j <= m; j++) d[0, j] = j;

		for (int i = 1; i <= n; i++)
		{
			for (int j = 1; j <= m; j++)
			{
				int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
				d[i, j] = Math.Min(
					Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
					d[i - 1, j - 1] + cost
				);
			}
		}

		return d[n, m];
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