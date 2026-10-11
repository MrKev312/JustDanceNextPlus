using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public interface IModernJduMenuService
{
    JduHomeDataResponse CreateHomeData(EditorialAudience audience);
    JduHomeDataResponse CreateHomeData(EditorialAudience audience, JduHomeRequest request, IReadOnlyList<MenuRow> rows);
    JduPlaylistDbResponse CreatePlaylistDatabase(int? mapCount = null);
    JduCarouselContentResponse CreatePlaylistCarousel();
    JduAnthologyCarouselContentResponse CreateAnthologyCarousel();
}

public sealed class ModernJduMenuService(
    IMapService mapService,
    IPlaylistService playlistService,
    IEditorialService editorialService,
    TimeProvider? timeProvider = null) : IModernJduMenuService
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    public const string PlaceholderText = "this is a test";

    public JduHomeDataResponse CreateHomeData(EditorialAudience audience)
    {
        DateTimeOffset now = clock.GetUtcNow();
        string? firstMapName = GetMapNames().FirstOrDefault();
        ServerPlaylist? firstPlaylist = GetPlaylists().FirstOrDefault();

        List<JduHomeTile> tiles = [.. editorialService.GetEntries(audience)
            .Where(entry => entry.ShowOnHome)
            .Select(entry => new JduHomeTile
            {
                Type = 2,
                CreationTime = entry.PublicationDate.ToUnixTimeMilliseconds(),
                New = true,
                Uuid = entry.Id,
                ContentExpiry = entry.ExpirationDate?.ToUnixTimeMilliseconds() ?? 0,
                NewsTileInfo = new JduNewsTileInfo
                {
                    Title = entry.Title,
                    Text = entry.Body,
                    ImageUrl = entry.ImageUrl
                }
            })];

        if (firstMapName is not null)
        {
            tiles.Add(new JduHomeTile
            {
                Type = 0,
                CreationTime = 0,
                Uuid = StableContentId.Create($"home:map:{firstMapName}"),
                New = true,
                MapTileInfo = new JduMapTileInfo { MapName = firstMapName }
            });

        }

        if (firstPlaylist is not null)
        {
            tiles.Add(new JduHomeTile
            {
                Type = 1,
                CreationTime = 0,
                Uuid = StableContentId.Create($"home:playlist:{firstPlaylist.Id:D}"),
                New = true,
                PlaylistTileInfo = new JduPlaylistTileInfo
                {
                    PlaylistID = GetPlaylistId(firstPlaylist)
                }
            });
        }

        long timestamp = now.ToUnixTimeSeconds();
        return new JduHomeDataResponse
        {
            TileList = [.. tiles],
            TimestampLastRequest = timestamp,
            TimestampLastManualContent = timestamp,
            TimestampLastMapContent = timestamp,
            TimestampLastPlaylistContent = timestamp
        };
    }

    public JduHomeDataResponse CreateHomeData(EditorialAudience audience, JduHomeRequest request, IReadOnlyList<MenuRow> rows)
    {
        JduHomeDataResponse baseline = CreateHomeData(audience);
        HashSet<Guid> history = [.. request.TileHistory.Where(tile => tile is not null).Select(tile => tile.Uuid)];
        IEnumerable<JduHomeTile> recommendations = rows.SelectMany(row => row.MapNames).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new JduHomeTile
            {
                Type = 0,
                Uuid = StableContentId.Create($"home:map:{name}"),
                MapTileInfo = new JduMapTileInfo { MapName = name }
            });
        JduHomeTile[] tiles = [.. baseline.TileList.Where(tile => tile.Type != 0)
            .Concat(recommendations).Concat(baseline.TileList.Where(tile => tile.Type == 0))
            .DistinctBy(tile => tile.Uuid).OrderBy(tile => history.Contains(tile.Uuid))
            .Take(Math.Clamp(request.RequestedTileCount, 0, 100))
            .Select(tile => tile with { New = !history.Contains(tile.Uuid) })];
        long now = clock.GetUtcNow().ToUnixTimeSeconds();
        long Watermark(int type, long previous)
            => tiles.Any(tile => tile.Type == type) ? now : Math.Clamp(previous, 0, now);
        // tileHistory is delivery history, not read/dismiss state. No video tiles
        // are currently authored, so unsolicited watchedVideos create no state.
        return baseline with
        {
            TileList = tiles,
            TimestampLastRequest = now,
            TimestampLastManualContent = Watermark(2, request.TimestampLastManualContent),
            TimestampLastMapContent = Watermark(0, request.TimestampLastMapContent),
            TimestampLastPlaylistContent = Watermark(1, request.TimestampLastPlaylistContent),
            TimestampLastVideoContent = Math.Clamp(request.TimestampLastVideoContent, 0, now),
            TimestampLastLocalTrack = Math.Clamp(request.TimestampLastLocalTrack, 0, now)
        };
    }

    public JduPlaylistDbResponse CreatePlaylistDatabase(int? mapCount = null)
    {
        int? normalizedMapCount = mapCount is > 0 ? mapCount : null;

        return new JduPlaylistDbResponse
        {
            Db = GetPlaylists().ToDictionary(
                GetPlaylistId,
                playlist => new JduPlaylist
                {
                    Maps = normalizedMapCount.HasValue
                        ? [.. playlist.MapNames.Take(normalizedMapCount.Value)]
                        : playlist.MapNames,
                    Title = playlist.Title,
                    Description = playlist.Description,
                    FixedMapOrder = true
                },
                StringComparer.OrdinalIgnoreCase)
        };
    }

    public JduCarouselContentResponse CreatePlaylistCarousel()
    {
        JduCarouselItem[] items =
        [
            .. GetPlaylists().Select(playlist => new JduCarouselItem
            {
                Components =
                [
                    new JduCarouselPlaylistComponent
                    {
                        PlaylistID = GetPlaylistId(playlist)
                    }
                ]
            })
        ];

        return new JduCarouselContentResponse
        {
            Categories =
            [
                new JduCarouselCategory
                {
                    Title = "Playlists",
                    Items = items
                }
            ],
            ActionLists = new Dictionary<string, JduCarouselActionList>
            {
                ["_None"] = new JduCarouselActionList
                {
                    Actions = [new JduCarouselAction()]
                }
            }
        };
    }

    public JduAnthologyCarouselContentResponse CreateAnthologyCarousel()
    {
        JduAnthologyCarouselItem[] items = [.. GetMapNames()
            .Select((mapName, index) => new JduAnthologyCarouselItem
            {
                Isc = $"anthology_planet_item_{index}",
                Components = [new JduCarouselSongComponent { MapName = mapName }]
            })];

        return new JduAnthologyCarouselContentResponse
        {
            Categories =
            [
                new JduAnthologyCarouselCategory
                {
                    Title = PlaceholderText,
                    Items = items
                }
            ],
            ActionLists = new Dictionary<string, JduCarouselActionList>
            {
                ["_None"] = new JduCarouselActionList
                {
                    Actions = [new JduCarouselAction()]
                }
            }
        };
    }

    private string[] GetMapNames()
    {
        return [.. JduCatalogSelection.From(mapService.Maps.Values.Select(map => map.Song))
            .Select(song => song.MapName)
            .Where(mapName => !string.IsNullOrWhiteSpace(mapName))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private ServerPlaylist[] GetPlaylists()
        =>
        [
            .. playlistService.Playlists.Values
                .Where(playlist => playlist.MapNames.Length > 1)
                .OrderBy(playlist => playlist.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(playlist => playlist.Id)
        ];

    private static string GetPlaylistId(ServerPlaylist playlist)
        => playlist.Id.ToString("D");
}
