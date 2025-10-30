using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Text.RegularExpressions;

namespace JustDanceNextPlus.Services;

public interface IMapService : ILoadService
{
    OrderedDictionary<Guid, JustDanceSongDBEntry> Songs { get; }
    OrderedDictionary<Guid, ContentAuthorization> ContentAuthorization { get; }
    SongDBTypeSet SongDBTypeSet { get; }
    OrderedDictionary<Guid, Dictionary<string, AssetMetadata>> AssetMetadataPerSong { get; }

    Dictionary<string, Guid> MapToGuid { get; }
    List<MapTag> RecentlyAdded { get; }

    Guid? GetSongId(string mapName);
}

public partial class MapService(IOptions<PathSettings> pathSettings,
	IOptions<UrlSettings> urlSettings,
	IServiceProvider serviceProvider,
	ILogger<MapService> logger,
	IFileSystem fileSystem) : IMapService, ILoadService
{
	private readonly PathSettings settings = pathSettings.Value;
    public OrderedDictionary<Guid, JustDanceSongDBEntry> Songs { get; set; } = [];
    public OrderedDictionary<Guid, ContentAuthorization> ContentAuthorization { get; set; } = [];
    public SongDBTypeSet SongDBTypeSet { get; set; } = new(urlSettings.Value.HostUrl);
    public OrderedDictionary<Guid, Dictionary<string, AssetMetadata>> AssetMetadataPerSong { get; set; } = [];
	public Dictionary<string, Guid> MapToGuid { get; } = [];
	public List<MapTag> RecentlyAdded { get; private set; } = [];

    public async Task LoadData()
	{
		try
		{
            await LoadMapsAsync();
			await LoadOffers();
			RecentlyAdded = await LoadRecentlyAddedMaps();
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
        ITagService tagService = serviceProvider.GetRequiredService<ITagService>();
        IUtilityService utilityService = serviceProvider.GetRequiredService<IUtilityService>();

		string[] mapFolders = [.. fileSystem.GetDirectories(settings.MapsPath).Select(x => Path.Combine(settings.MapsPath, Path.GetFileName(x)))];

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
			Songs[result.SongID] = result.SongInfo;
			ContentAuthorization[result.SongID] = result.ContentAuthorization;
			AssetMetadataPerSong[result.SongID] = result.AssetMetadata;
			SongDBTypeSet.SongOffers.DownloadableSongs.Add(result.SongID);
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
            if (result.SongInfo.CoachCount is >= 1 and <= 4)
            {
                string[] playerCounts = ["Solo", "Duet", "Trio", "Quartet"];
                Tag tag = tagService.GetAddTag(playerCounts[result.SongInfo.CoachCount - 1], "choreoSettings");
                result.SongInfo.TagIds.Add(tag);
            }
        }
    }

	public async Task LoadOffers()
	{
        // Now we load the bundle service to get the offers
		IBundleService bundleService = serviceProvider.GetRequiredService<IBundleService>();
		await bundleService.LoadData();

        Dictionary<string, Pack> packs = [];

		foreach (KeyValuePair<Guid, JustDanceSongDBEntry> song in Songs)
		{
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
		SongDBTypeSet.SongOffers.Claims = packs
			.OrderBy(x => x.Key.StartsWith("jdplus") ? 0 : x.Key.StartsWith("songpack_") ? 1 : x.Key.StartsWith("welcomeGifts") ? 2 : 3)
			.ThenBy(x => x.Key)
			.ToDictionary(x => x.Key, x => x.Value);

		return;
	}

    public Task<List<MapTag>> LoadRecentlyAddedMaps()
    {
		// Get all maps in the maps folder and their creation time
		IOrderedEnumerable<(string MapName, DateTime CreationTime)> mapCreationTimes = fileSystem.GetDirectories(settings.MapsPath)
            .Select(dir => (MapName: Path.GetFileName(dir), CreationTime: fileSystem.GetDirectoryCreationTime(dir)))
            .OrderBy(x => x.CreationTime);

        Stack<(string MapName, DateTime CreationTime)> mapStack = new(mapCreationTimes);

        DateTime prevDateTime = mapStack.Peek().CreationTime;

		List<string> recentlyAddedCodenames = [];

        // We need at least 4, and all that are within 1 hour of the last one
        while (mapStack.Count > 0)
        {
            (string MapName, DateTime CreationTime) = mapStack.Pop();

            if (recentlyAddedCodenames.Count >= 4 && (prevDateTime - CreationTime).TotalHours > 1)
                break;

            prevDateTime = CreationTime;
            recentlyAddedCodenames.Add(MapName);
        }

        // Log the recently added codenames
		logger.LogInformation("Recently added maps: {Maps}", string.Join(", ", recentlyAddedCodenames));

        // Convert to song IDs, ignoring those that don't exist
        List<MapTag> recentlyAddedSongs = [.. recentlyAddedCodenames.Select(GetSongId)
			.Where(id => id.HasValue)
			.Select(id => id!.Value)];

        return Task.FromResult(recentlyAddedSongs);
    }

    private static async Task<(LocalJustDanceSongDBEntry, ContentAuthorization, Dictionary<string, AssetMetadata>)> LoadMapAsync(string mapFolder, IUtilityService utilityService)
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
