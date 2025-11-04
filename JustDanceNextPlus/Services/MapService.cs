using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace JustDanceNextPlus.Services;

public interface IMapService : ILoadService
{
    OrderedDictionary<Guid, JustDanceSongDBEntry> Songs { get; }
    OrderedDictionary<Guid, ContentAuthorization> ContentAuthorization { get; }
    SongDBTypeSet SongDBTypeSet { get; }
    OrderedDictionary<Guid, IReadOnlyDictionary<string, AssetMetadata>> AssetMetadataPerSong { get; }

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
    private readonly UrlSettings urls = urlSettings.Value;
    public OrderedDictionary<Guid, JustDanceSongDBEntry> Songs { get; set; } = [];
    public OrderedDictionary<Guid, ContentAuthorization> ContentAuthorization { get; set; } = [];
    public SongDBTypeSet SongDBTypeSet { get; private set; } = new(urlSettings.Value.HostUrl);
    public OrderedDictionary<Guid, IReadOnlyDictionary<string, AssetMetadata>> AssetMetadataPerSong { get; set; } = [];
    public Dictionary<string, Guid> MapToGuid { get; } = [];
    public List<MapTag> RecentlyAdded { get; private set; } = [];

    public async Task LoadData()
    {
        try
        {
			IReadOnlySet<Guid> songGuids = await LoadMapsAsync();
			IReadOnlyDictionary<string, Pack> claims = await LoadOffers();
            RecentlyAdded = await LoadRecentlyAddedMaps();

			Songoffers songOffers = new()
			{
                DownloadableSongs = songGuids,
                Claims = claims
            };

            SongDBTypeSet = new SongDBTypeSet(urls.HostUrl) with { SongOffers = songOffers };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading data");
            throw;
        }
    }

    private async Task<IReadOnlySet<Guid>> LoadMapsAsync()
    {
        // Load the tag service
        ITagService tagService = serviceProvider.GetRequiredService<ITagService>();
        IUtilityService utilityService = serviceProvider.GetRequiredService<IUtilityService>();

        string[] mapFolders = [.. fileSystem.GetDirectories(settings.MapsPath).Select(x => Path.Combine(settings.MapsPath, Path.GetFileName(x)))];

        var loadMapTasks = mapFolders.Select(async mapFolder =>
        {
            (LocalJustDanceSongDBEntry songInfo, ContentAuthorization contentAuthorization, IReadOnlyDictionary<string, AssetMetadata> assetMetadata) = await LoadMapAsync(mapFolder, utilityService);
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
		ImmutableHashSet<Guid>.Builder downloadableSongsBuilder = ImmutableHashSet.CreateBuilder<Guid>();

        // Add the songs and content authorizations to the song database
        foreach (var result in mapResults)
        {
            Guid songID = result.SongID;
            LocalJustDanceSongDBEntry songInfo = result.SongInfo;
			ContentAuthorization contentAuthorization = result.ContentAuthorization;
            IReadOnlyDictionary<string, AssetMetadata> assetMetadata = result.AssetMetadata;

            // Split the artist by & and trim the results
            Regex regex = ArtistSplitRegex();
            string[] artists = [.. regex.Split(result.SongInfo.Artist).Select(x => x.Trim())];

            foreach (string artist in artists)
            {
                // Add the artist to the tag service
                Tag tag = tagService.GetAddTag(artist, "artist");
                songInfo = songInfo with
                {
                    TagIds = songInfo.TagIds.Add(tag)
                };
            }

            // Process player count
            if (songInfo.CoachCount is >= 1 and <= 4)
            {
                string[] playerCounts = ["Solo", "Duet", "Trio", "Quartet"];
                Tag tag = tagService.GetAddTag(playerCounts[songInfo.CoachCount - 1], "choreoSettings");
                songInfo = songInfo with
                {
                    TagIds = songInfo.TagIds.Add(tag)
                };
            }

            Songs[songID] = songInfo;
            ContentAuthorization[songID] = contentAuthorization;
            AssetMetadataPerSong[songID] = assetMetadata;
            downloadableSongsBuilder.Add(songID);
            MapToGuid[songInfo.MapName] = songID;
        }

        return downloadableSongsBuilder.ToImmutable();
    }

    private async Task<IReadOnlyDictionary<string, Pack>> LoadOffers()
    {
        // Now we load the bundle service to get the offers
        IBundleService bundleService = serviceProvider.GetRequiredService<IBundleService>();
        await bundleService.LoadData();

        Dictionary<string, (int GroupLocId, List<Guid> SongIds, string Tag)> tempPacks = [];

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

                if (!tempPacks.TryGetValue(tag, out (int GroupLocId, List<Guid> SongIds, string Tag) packData))
                {
                    packData = (groupLocId, new List<Guid>(), tag);
                    tempPacks[tag] = packData;
                }

                packData.SongIds.Add(song.Key);
            }
        }

        Dictionary<string, Pack> packs = tempPacks.ToDictionary(
            kvp => kvp.Key,
            kvp => new Pack
            {
                DescriptionLocId = kvp.Value.GroupLocId,
                AllowSharing = true,
                FreeTrialDurationMinutes = 43200,
                SongPackIds = ImmutableHashSet<string>.Empty,
                UnlocksFullVersion = !kvp.Value.Tag.StartsWith("Music_Pack_"),
                SongIds = kvp.Value.SongIds.ToImmutableHashSet()
            });

        // Add welcomeGifts pack
        if (!packs.ContainsKey("welcomeGifts"))
        {
            packs["welcomeGifts"] = new Pack
            {
                RewardIds = ImmutableHashSet<Guid>.Empty,
            };
        }

        // Convert to array and sort by tag, then return as an immutable dictionary
        return packs
            .OrderBy(x => x.Key.StartsWith("jdplus") ? 0 : x.Key.StartsWith("songpack_") ? 1 : x.Key.StartsWith("welcomeGifts") ? 2 : 3)
            .ThenBy(x => x.Key)
            .ToImmutableDictionary(x => x.Key, x => x.Value);
    }

    public Task<List<MapTag>> LoadRecentlyAddedMaps()
    {
        // Get all maps in the maps folder and their creation time
        IOrderedEnumerable<(string MapName, DateTime CreationTime)> mapCreationTimes = fileSystem.GetDirectories(settings.MapsPath)
            .Select(dir => (MapName: Path.GetFileName(dir), CreationTime: fileSystem.GetDirectoryCreationTime(dir)))
            .OrderBy(x => x.CreationTime);

        if (!mapCreationTimes.Any())
        {
            logger.LogWarning("No maps found in {MapsPath}", settings.MapsPath);
            return Task.FromResult(new List<MapTag>());
        }

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

    private static async Task<(LocalJustDanceSongDBEntry, ContentAuthorization, IReadOnlyDictionary<string, AssetMetadata>)> LoadMapAsync(string mapFolder, IUtilityService utilityService)
    {
        LocalJustDanceSongDBEntry songInfo = await utilityService.LoadMapDBEntryAsync(mapFolder);
        ContentAuthorization contentAuthorization = utilityService.LoadContentAuthorization(mapFolder);
        IReadOnlyDictionary<string, AssetMetadata> assetMetadata = utilityService.LoadAssetMetadata(mapFolder);

        return (songInfo, contentAuthorization, assetMetadata);
    }

    public Guid? GetSongId(string mapName)
    {
        return MapToGuid.TryGetValue(mapName, out Guid songId)
            ? songId
            : null;
    }

    [GeneratedRegex(@"(?: & | ft. | feat. | featuring )", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ArtistSplitRegex();
}
