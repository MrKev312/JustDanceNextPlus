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

		// Bool for partial match
		bool partialMatchTitle = song.Title.Contains(searchInput, StringComparison.OrdinalIgnoreCase);
		bool partialMatchArtist = song.Artist.Contains(searchInput, StringComparison.OrdinalIgnoreCase);

		// Helper function to check if two strings are one character off (including insertions/deletions)
		static bool IsOneCharOff(string s1, string s2)
		{
			s2 = s2.ToLowerInvariant();

			int len1 = s1.Length;
			int len2 = s2.Length;

			// If the length difference is greater than 1, it's more than one char off
			if (Math.Abs(len1 - len2) > 1)
				return false;

			int mismatchCount = 0;
			int i = 0, j = 0;

			// Walk through both strings and count mismatches
			while (i < len1 && j < len2)
			{
				if (s1[i] != s2[j])
				{
					mismatchCount++;
					if (mismatchCount > 1)
						return false;

					// If lengths differ, move the pointer of the longer string ahead
					if (len1 > len2)
						i++;
					else if (len2 > len1)
						j++;
					else
					{
						i++;
						j++;
					}
				}
				else
				{
					i++;
					j++;
				}
			}

			// If one string is longer, count that as an additional mismatch
			if (i < len1 || j < len2)
				mismatchCount++;

			return mismatchCount == 1;
		}

		// Exact year match
		if (uint.TryParse(searchInput, out uint searchNumber) && song.OriginalJDVersion == searchNumber)
			relevance += 100;

		// Title matches
		if (partialMatchTitle && searchInput.Length == song.Title.Length)
			relevance += 10; // Exact title match
		if (partialMatchTitle)
			relevance += 7; // Partial title match
		else if (IsOneCharOff(searchInput, song.Title))
			relevance += 5; // One character off from title

		// Artist matches
		if (partialMatchArtist && searchInput.Length == song.Artist.Length)
			relevance += 5; // Exact artist match
		else if (IsOneCharOff(searchInput, song.Artist))
			relevance += 2; // One character off from artist
		if (partialMatchArtist)
			relevance += 1; // Partial artist match

		return relevance;
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