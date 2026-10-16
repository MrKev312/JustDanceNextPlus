using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Options;

using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace JustDanceServer.Services;

public interface IMapService : ILoadService
{
    OrderedDictionary<Guid, ServerMap> Maps { get; }
    OrderedDictionary<Guid, JustDanceSongDBEntry> GetSongs(
        string platform,
        MapMediaLayout previewLayout);
    ContentAuthorization? GetContentAuthorization(string platform, string mapName, int qualityCount);
    SongDBTypeSet SongDBTypeSet { get; }
    OrderedDictionary<Guid, IReadOnlyDictionary<string, AssetMetadata>> GetAssetMetadataPerSong(
        string platform,
        MapMediaLayout previewLayout);

    Dictionary<string, Guid> MapToGuid { get; }
    List<MapTag> RecentlyAdded { get; }

    Guid? GetSongId(string mapName);
    ServerMap? GetMap(string mapName);
    bool TryGetMapName(Guid songId, out string mapName);
}

public partial class MapService(IOptions<PathSettings> pathSettings,
    IOptions<UrlSettings> urlSettings,
    IServiceProvider serviceProvider,
    ILogger<MapService> logger,
    IFileSystem fileSystem) : IMapService, ILoadService
{
    private readonly Lock loadLock = new();
    private Task? loadTask;
    private readonly PathSettings settings = pathSettings.Value;
    private readonly UrlSettings urls = urlSettings.Value;
    public OrderedDictionary<Guid, ServerMap> Maps { get; } = [];
    public SongDBTypeSet SongDBTypeSet { get; private set; } = new(urlSettings.Value.HostUrl);
    public Dictionary<string, Guid> MapToGuid { get; } = [];
    public List<MapTag> RecentlyAdded { get; private set; } = [];
    private ConcurrentDictionary<MapAssetMetadataCacheKey, Lazy<OrderedDictionary<Guid, IReadOnlyDictionary<string, AssetMetadata>>>> AssetMetadataCache { get; } = [];

    public Task LoadData()
    {
        lock (loadLock)
            return loadTask ??= LoadDataCore();
    }

    private async Task LoadDataCore()
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
        IServerMapLoader mapLoader = serviceProvider.GetRequiredService<IServerMapLoader>();

        Maps.Clear();
        MapToGuid.Clear();
        AssetMetadataCache.Clear();

        string[] mapFolders = [.. fileSystem.GetDirectories(settings.MapsPath).Select(x => Path.Combine(settings.MapsPath, Path.GetFileName(x)))];

        IEnumerable<Task<ServerMap>> loadMapTasks = mapFolders
            .Select(mapFolder => mapLoader.LoadMapAsync(mapFolder).AsTask());

        // Sort the results by SongID
        IEnumerable<ServerMap> mapResults = (await Task.WhenAll(loadMapTasks))
            .OrderBy(map => map.SongId);
        ImmutableHashSet<Guid>.Builder downloadableSongsBuilder = ImmutableHashSet.CreateBuilder<Guid>();

        foreach (ServerMap map in mapResults)
        {
            Guid songID = map.SongId;
            JustDanceSongDBEntry songInfo = map.Song;

            // Split the artist by & and trim the results
            Regex regex = ArtistSplitRegex();
            string[] artists = [.. regex.Split(songInfo.Artist).Select(x => x.Trim())];

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

            Maps[songID] = map with { Song = songInfo };
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

        foreach ((Guid songId, ServerMap map) in Maps)
        {
            foreach (string tag in map.Song.Tags)
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

                packData.SongIds.Add(songId);
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

    public Guid? GetSongId(string mapName)
    {
        return MapToGuid.TryGetValue(mapName, out Guid songId)
            ? songId
            : null;
    }

    public ServerMap? GetMap(string mapName)
    {
        Guid? songId = GetSongId(mapName);
        return songId.HasValue && Maps.TryGetValue(songId.Value, out ServerMap? map)
            ? map
            : null;
    }

    public bool TryGetMapName(Guid songId, out string mapName)
    {
        if (Maps.TryGetValue(songId, out ServerMap? map))
        {
            mapName = map.Song.MapName;
            return true;
        }

        mapName = string.Empty;
        return false;
    }

    public OrderedDictionary<Guid, JustDanceSongDBEntry> GetSongs(
        string platform,
        MapMediaLayout previewLayout)
    {
        string normalizedPlatform = ClientPlatform.Normalize(platform);
        OrderedDictionary<Guid, JustDanceSongDBEntry> platformSongs = [];

        foreach ((Guid songId, ServerMap map) in Maps)
        {
            MapMediaAssets media = map.GetMedia(MapMediaLayouts.JDPlus);
            MapMediaAssets previewMedia = map.GetMedia(previewLayout);
            platformSongs[songId] = map.Song with
            {
                AssetsMetadata = map.Song.AssetsMetadata with
                {
                    VideoDataVp8 = [.. previewMedia.GetPreviewVideos("vp8").Qualities.Select(quality => quality.Data)],
                    VideoData = [.. previewMedia.GetPreviewVideos("vp9").Qualities.Select(quality => quality.Data)]
                },
                Assets = ProjectJdPlusSongAssets(
                    media,
                    previewMedia,
                    normalizedPlatform,
                    map.Song.MapName)
            };
        }

        return platformSongs;
    }

    public ContentAuthorization? GetContentAuthorization(string platform, string mapName, int qualityCount)
    {
        ServerMap? map = GetMap(mapName);

        if (map is null)
            return null;

        string normalizedPlatform = ClientPlatform.Normalize(platform);
        MapVideoCodec preferredCodec = ClientPlatform.GetVideoCodec(normalizedPlatform);

        // Validate the requested wire layout before constructing its asset projection.
        _ = VideoQualityLayout.FullVideoAssetNames(preferredCodec, qualityCount);

        MapVideoSet vp8Videos = map.GetFullVideos("vp8").WithQualityCount(
            preferredCodec == MapVideoCodec.Vp8 ? qualityCount : VideoQualityLayout.Standard.Count);
        MapVideoSet vp9Videos = map.GetFullVideos("vp9").WithQualityCount(
            preferredCodec == MapVideoCodec.Vp9 ? qualityCount : VideoQualityLayout.Standard.Count);

        MapMediaAssets media = map.GetMedia(MapMediaLayouts.JDPlus);
        IReadOnlyDictionary<string, string> assets = media.GetPlatformAssets(normalizedPlatform);

        return new ContentAuthorization
        {
            AssetsMetadata = new AssetsmetadataAuthorization
            {
                VideoSet = preferredCodec == MapVideoCodec.Vp8 ? vp8Videos : vp9Videos
            },
            Assets = ProjectJdPlusContentAuthorizationAssets(
                media, vp8Videos, vp9Videos, assets, map.Song.MapName, normalizedPlatform)
        };
    }

    public OrderedDictionary<Guid, IReadOnlyDictionary<string, AssetMetadata>> GetAssetMetadataPerSong(
        string platform,
        MapMediaLayout previewLayout)
    {
        string normalizedPlatform = ClientPlatform.Normalize(platform);
        MapAssetMetadataCacheKey cacheKey = new(normalizedPlatform, previewLayout.FolderName);
        return AssetMetadataCache.GetOrAdd(cacheKey, _ => new(() =>
        {
            OrderedDictionary<Guid, IReadOnlyDictionary<string, AssetMetadata>> platformMetadata = [];
            IJdPlusAssetMetadataLoader metadataLoader = serviceProvider.GetRequiredService<IJdPlusAssetMetadataLoader>();
            foreach ((Guid songId, ServerMap map) in Maps)
                platformMetadata[songId] = metadataLoader.Load(map.FolderPath, normalizedPlatform, previewLayout);
            return platformMetadata;
        }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    private static SongDBAssets ProjectJdPlusSongAssets(
        MapMediaAssets media,
        MapMediaAssets previewMedia,
        string platform,
        string mapName)
    {
        IReadOnlyDictionary<string, string> platformAssets = media.GetPlatformAssets(platform);
        IReadOnlyList<MapVideoQuality> vp8 = previewMedia.GetPreviewVideos("vp8").Qualities;
        IReadOnlyList<MapVideoQuality> vp9 = previewMedia.GetPreviewVideos("vp9").Qualities;
        SongDBAssets assets = new()
        {
            AudioPreview_opus = media.PreviewAudioUrl,
            VideoPreview_LOW_vp8_webm = vp8[0].Url,
            VideoPreview_MID_vp8_webm = vp8[1].Url,
            VideoPreview_HIGH_vp8_webm = vp8[2].Url,
            VideoPreview_ULTRA_vp8_webm = vp8[3].Url,
            VideoPreview_LOW_vp9_webm = vp9[0].Url,
            VideoPreview_MID_vp9_webm = vp9[1].Url,
            VideoPreview_HIGH_vp9_webm = vp9[2].Url,
            VideoPreview_ULTRA_vp9_webm = vp9[3].Url,
            CoachesLarge = RequireAsset(platformAssets, "CoachesLarge", mapName, platform),
            CoachesSmall = RequireAsset(platformAssets, "CoachesSmall", mapName, platform),
            Cover = RequireAsset(platformAssets, "Cover", mapName, platform),
            SongTitleLogo = platformAssets.GetValueOrDefault("songTitleLogo")
        };

        return assets;
    }

    private static AssetsAuthorization ProjectJdPlusContentAuthorizationAssets(
        MapMediaAssets media,
        MapVideoSet vp8Videos,
        MapVideoSet vp9Videos,
        IReadOnlyDictionary<string, string> platformAssets,
        string mapName,
        string platform)
    {
        IReadOnlyList<MapVideoQuality> vp8 = vp8Videos.Qualities;
        IReadOnlyList<MapVideoQuality> vp9 = vp9Videos.Qualities;
        return new AssetsAuthorization
        {
            AudioOpus = media.FullAudioUrl,
            VideoLowHdWebm = vp8[0].Url,
            VideoLowVP9Webm = vp9[0].Url,
            VideoMidHdWebm = vp8[1].Url,
            VideoMidVP9Webm = vp9[1].Url,
            VideoHighHdWebm = vp8[2].Url,
            VideoHighVP9Webm = vp9[2].Url,
            VideoUltraHdWebm = vp8[3].Url,
            VideoUltraVP9Webm = vp9[3].Url,
            VideoUltraHdVP9Webm = vp9.Count > 4 ? vp9[4].Url : null,
            MapPackage = RequireAsset(platformAssets, "MapPackage", mapName, platform)
        };
    }

    private static string RequireAsset(
        IReadOnlyDictionary<string, string> assets,
        string role,
        string mapName,
        string platform)
        => assets.TryGetValue(role, out string? url)
            ? url
            : throw new InvalidDataException(
                $"Map '{mapName}' is missing required {role} assets for {platform}.");

    [GeneratedRegex(@"(?: & | ft. | feat. | featuring )", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ArtistSplitRegex();

    private readonly record struct MapAssetMetadataCacheKey(string Platform, string PreviewLayout);
}
