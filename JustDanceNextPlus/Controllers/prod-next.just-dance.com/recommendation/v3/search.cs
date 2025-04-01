using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;
using Microsoft.AspNetCore.Mvc;
using FuzzySharp;

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

		// Search the database using fuzzy matching to compute a relevance score.
		var searchSongResult = mapService.SongDB.Songs
			.AsParallel()
			.Select(song => new
			{
				Song = song,
				// Calculate relevance using fuzzy logic (see GetRelevance below)
				Relevance = GetRelevance(normalizedSearchInput, song.Value)
			})
			.Where(song => song.Relevance >= 50)
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

	/// <summary>
	/// Calculate a combined fuzzy relevance score using FuzzySharp.
	/// </summary>
	static float GetRelevance(string searchInput, JustDanceSongDBEntry song)
	{
		float relevance = 0;

		// Bonus: Exact match on year gets extra points.
		if (uint.TryParse(searchInput, out uint searchNumber) && song.OriginalJDVersion == searchNumber)
			relevance += 50;

		// Calculate fuzzy similarity scores using WeightedRatio.
		// Higher weight for title, lower for artist.
		int titleScore = Fuzz.WeightedRatio(searchInput, song.Title);
		int artistScore = Fuzz.WeightedRatio(searchInput, song.Artist);

		// Combine the scores with chosen weights.
		// For example: title weight 1.0, artist weight 0.5.
		relevance += (titleScore + (artistScore * 0.5f)) / 1.5f;

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
