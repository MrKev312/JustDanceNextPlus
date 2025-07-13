using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Text.RegularExpressions;

namespace JustDanceNextPlus.Services;

public partial class MapService(IOptions<PathSettings> pathSettings,
	IOptions<UrlSettings> urlSettings,
	IServiceProvider serviceProvider,
	ILogger<MapService> logger) : ILoadService
{
	private readonly PathSettings settings = pathSettings.Value;

	public JustDanceSongDB SongDB { get; private set; } = new(urlSettings.Value.HostUrl);
	public Dictionary<string, Guid> MapToGuid { get; } = [];

	public async Task LoadData()
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

		string[] mapFolders = [.. Directory.GetDirectories(settings.MapsPath).Select(x => Path.Combine(settings.MapsPath, Path.GetFileName(x)))];

		var loadMapTasks = mapFolders.Select(async mapFolder =>
		{
			(LocalJustDanceSongDBEntry songInfo, ContentAuthorization contentAuthorization, Dictionary<string, AssetMetadata> assetMetadata) = await LoadMapAsync(mapFolder, utilityService);
			return new
			{
				songInfo.SongID,
				SongInfo = songInfo,
				ContentAuthorization = contentAuthorization,
				AssetMetadata = assetMetadata
			};
		});

		// Sort the results by SongID
		var mapResults = (await Task.WhenAll(loadMapTasks)).OrderBy(result => result.SongID);

		// Add the songs and content authorizations to the song database
		foreach (var result in mapResults)
		{
			SongDB.Songs[result.SongID] = result.SongInfo;
			SongDB.ContentAuthorization[result.SongID] = result.ContentAuthorization;
			SongDB.AssetMetadataPerSong[result.SongID] = result.AssetMetadata;
			SongDB.SongDBTypeSet.SongOffers.DownloadableSongs.Add(result.SongID);
			MapToGuid[result.SongInfo.MapName] = result.SongID;

			// Split the artist by & and trim the results
			Regex regex = ArtistSplitRegex();
			string[] artists = [.. regex.Split(result.SongInfo.Artist).Select(x => x.Trim())];

			foreach (string artist in artists) 
			{
				// Add the artist to the tag service
				Tag tag = tagService.GetAddTag(artist, "artist");
				result.SongInfo.TagIds.Add(tag);
			}

			// Process player count
			{
				string[] playerCounts = ["Solo", "Duet", "Trio", "Quartet"];
				Tag tag = tagService.GetAddTag(playerCounts[result.SongInfo.CoachCount - 1], "choreoSettings");
				result.SongInfo.TagIds.Add(tag);
			}
		}
	}

	public Task LoadOffers()
	{
		BundleService bundleService = serviceProvider.GetRequiredService<BundleService>();

		Dictionary<string, Pack> packs = [];

		foreach (KeyValuePair<Guid, JustDanceSongDBEntry> song in SongDB.Songs)
		{
			bool addedToPack = false;

			foreach (string tag in song.Value.Tags)
			{
				if (tag != "jdplus" && !tag.StartsWith("songpack_") && !tag.StartsWith("Music_Pack_"))
					continue;

				Guid productGroupId;

				if (tag == "jdplus")
				{
					productGroupId = bundleService.ShopConfig.FirstPartyProductDb.ProductGroups
						.FirstOrDefault(x => x.Value.Type == "jdplus").Key;
				}
				else
				{

					Guid dlcId = bundleService.ShopConfig.FirstPartyProductDb.DlcProducts
						.FirstOrDefault(x => x.Value.ClaimIds.Contains(tag)).Key;

					productGroupId = bundleService.ShopConfig.FirstPartyProductDb.ProductGroups
						.FirstOrDefault(x => x.Value.ProductIds.Contains(dlcId)).Key;
				}

				if (productGroupId == Guid.Empty)
					continue;

				int groupLocId = bundleService.ShopConfig.FirstPartyProductDb.ProductGroups[productGroupId].GroupLocId;

				if (!packs.TryGetValue(tag, out Pack? pack))
				{
					pack = new Pack
					{
						DescriptionLocId = groupLocId,
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

			// If the song is not added to any pack, add it to the jdplus pack
			if (!addedToPack && !song.Value.Tags.Contains("jdplus"))
			{
				song.Value.Tags.Add("jdplus");

				Guid productGroupId = bundleService.ShopConfig.FirstPartyProductDb.ProductGroups
					.FirstOrDefault(x => x.Value.Type == "jdplus").Key;

				int groupLocId = bundleService.ShopConfig.FirstPartyProductDb.ProductGroups[productGroupId].GroupLocId;

				if (!packs.TryGetValue("jdplus", out Pack? pack))
				{
					pack = new Pack
					{
						DescriptionLocId = groupLocId,
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

	private static async Task<(LocalJustDanceSongDBEntry, ContentAuthorization, Dictionary<string, AssetMetadata>)> LoadMapAsync(string mapFolder, UtilityService utilityService)
	{
		LocalJustDanceSongDBEntry songInfo = await utilityService.LoadMapDBEntryAsync(mapFolder);
		ContentAuthorization contentAuthorization = utilityService.LoadContentAuthorization(mapFolder);
		Dictionary<string, AssetMetadata> assetMetadata = utilityService.LoadAssetMetadata(mapFolder);

		return (songInfo, contentAuthorization, assetMetadata);
	}

	public Guid? GetSongId(string mapName)
	{
		return MapToGuid.TryGetValue(mapName, out Guid songId) 
			? songId 
			: null;
	}

	[GeneratedRegex(@"(?: & | ft. | feat. | featuring )", RegexOptions.IgnoreCase, "en-NL")]
	private static partial Regex ArtistSplitRegex();
}
