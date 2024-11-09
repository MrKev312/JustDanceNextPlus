using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Text.RegularExpressions;

namespace JustDanceNextPlus.Services;

public class MapService(IOptions<PathSettings> pathSettings,
	IServiceProvider serviceProvider,
	ILogger<MapService> logger)
{
	private readonly PathSettings settings = pathSettings.Value;

	public JustDanceSongDB SongDB { get; private set; } = new();

	public async Task LoadDataAsync()
	{
		try
		{
			await LoadMapsAsync();
			await LoadOffers();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error loading data");
			throw;
		}
	}

	public async Task LoadMapsAsync()
	{
		// Load the tag service
		TagService tagService = serviceProvider.GetRequiredService<TagService>();
		UtilityService utilityService = serviceProvider.GetRequiredService<UtilityService>();

		string[] mapFolders = Directory.GetDirectories(settings.MapsPath)
			.Select(x => Path.Combine(settings.MapsPath, Path.GetFileName(x)))
			.ToArray();

		var loadMapTasks = mapFolders.Select(async mapFolder =>
		{
			(LocalJustDanceSongDBEntry songInfo, ContentAuthorization contentAuthorization) = await LoadMapAsync(mapFolder, utilityService);
			return new
			{
				songInfo.SongID,
				SongInfo = songInfo,
				ContentAuthorization = contentAuthorization
			};
		});

		var mapResults = await Task.WhenAll(loadMapTasks);

		// Sort the results by SongID
		var sortedResults = mapResults.OrderBy(result => result.SongID);

		// Add the songs and content authorizations to the song database
		foreach (var result in sortedResults)
		{
			SongDB.Songs[result.SongID] = result.SongInfo;
			SongDB.ContentAuthorization[result.SongID] = result.ContentAuthorization;

			// Split the artist by & and trim the results
			Regex regex = new(@"(?: & | ft. | feat. | featuring )", RegexOptions.IgnoreCase);
			string[] artists = regex.Split(result.SongInfo.Artist).Select(x => x.Trim()).ToArray();

			foreach (string artist in artists) 
			{
				// Add the artist to the tag service
				Guid tag = tagService.GetAddTag(artist, "artist");
				result.SongInfo.TagIds.Add(tag);
			}

			// Process player count
			{
				string[] playerCounts = ["Solo", "Duet", "Trio", "Quartet"];
				Guid tag = tagService.GetAddTag(playerCounts[result.SongInfo.CoachCount - 1], "choreoSettings");
				result.SongInfo.TagIds.Add(tag);
			}
		}
	}

	public Task LoadOffers()
	{
		// Todo: update this with a packService?
		Dictionary<string, int> packToLocId = new()
		{
			{ "jdplus", 0 },
			{ "songpack_year1", 4794 },
			{ "songpack_year2", 5224 },
			{ "songpack_year3", 8598 },
			{ "Music_Pack_AllOutFun", 8643 },
			{ "Music_Pack_DisneyVol1", 8642 },
			{ "Music_Pack_PopParty", 8642 }
		};

		Dictionary<string, Pack> packs = [];

		foreach (KeyValuePair<Guid, JustDanceSongDBEntry> song in SongDB.Songs)
		{
			bool addedToPack = false;

			foreach (string tag in song.Value.Tags)
			{
				if (tag != "jdplus" && !tag.StartsWith("songpack_") && !tag.StartsWith("Music_Pack_"))
					continue;

				if (!packs.TryGetValue(tag, out Pack? pack))
				{
					pack = new Pack
					{
						DescriptionLocId = packToLocId[tag],
						AllowSharing = true,
						FreeTrialDurationMinutes = 43200,
						SongPackIds = [],
						UnlocksFullVersion = !tag.StartsWith("Music_Pack_")
					};
					packs[tag] = pack;
				}

				pack.SongIds.Add(song.Key);
				addedToPack = true;
			}

			// Else just add it to the jdplus pack
			if (!addedToPack)
			{
				song.Value.Tags.Add("jdplus");

				if (!packs.TryGetValue("jdplus", out Pack? pack))
				{
					pack = new Pack
					{
						DescriptionLocId = packToLocId["jdplus"],
						AllowSharing = true,
						FreeTrialDurationMinutes = 43200,
						SongPackIds = [],
						UnlocksFullVersion = true
					};
					packs["jdplus"] = pack;
				}

				pack.SongIds.Add(song.Key);
			}
		}

		// Add welcomeGifts pack
		if (!packs.ContainsKey("welcomeGifts"))
		{
			packs["welcomeGifts"] = new Pack
			{
				RewardIds = [],
			};
		}

		// Convert to array and sort by tag, jdplus first, then songpacks, then by welcomeGifts, then by music packs
		SongDB.SongDBTypeSet.SongOffers.Claims = packs
			.OrderBy(x => x.Key.StartsWith("jdplus") ? 0 : x.Key.StartsWith("songpack_") ? 1 : x.Key.StartsWith("welcomeGifts") ? 2 : 3)
			.ThenBy(x => x.Key)
			.ToDictionary(x => x.Key, x => x.Value);

		return Task.CompletedTask;
	}

	private async Task<(LocalJustDanceSongDBEntry, ContentAuthorization)> LoadMapAsync(string mapFolder, UtilityService utilityService)
	{
		LocalJustDanceSongDBEntry songInfo = await utilityService.LoadMapDBEntryAsync(mapFolder);
		ContentAuthorization contentAuthorization = utilityService.LoadContentAuthorization(mapFolder);

		return (songInfo, contentAuthorization);
	}
}
