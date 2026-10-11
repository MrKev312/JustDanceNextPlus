using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public interface IClassicJduMenuService
{
    JduSongCarouselContentResponse CreatePage(
        string pageName,
        ClassicJduClientProfile client,
        JduCarouselRequest? request = null);
}

public sealed class ClassicJduMenuService(IMapService mapService) : IClassicJduMenuService
{
    public JduSongCarouselContentResponse CreatePage(
        string pageName,
        ClassicJduClientProfile client,
        JduCarouselRequest? request = null)
    {
        JustDanceSongDBEntry[] songs = FilterSongs(GetSubscriptionSongs(client), request);
        JustDanceSongDBEntry[] allSongs = FilterSongs(GetAllSongs(), request);
        return pageName.ToLowerInvariant() switch
        {
            "alphabet" => CreateResponse(CreateAlphabetCategories(songs)),
            "originaljdversion" or "original-jd-version" =>
                CreateResponse(CreateOriginalVersionCategories(songs, client)),
            "kids" or "kids-extended" when client.GameYear == 2018 =>
                CreateKidsResponse(),
            "kids" or "kids-extended" => CreateResponse([]),
            "partycoop" => CreatePartyResponse(client, "partycoop", songs, allSongs, "partyMapCoop"),
            "sweat" => CreatePartyResponse(client, "sweat", songs, allSongs, "sweatMap"),
            _ => CreatePartyResponse(client, "party", songs, allSongs, "partyMap")
        };
    }

    private static JduSongCarouselContentResponse CreatePartyResponse(
        ClassicJduClientProfile client,
        string pageName,
        JustDanceSongDBEntry[] songs,
        JustDanceSongDBEntry[] allSongs,
        string actionList)
    {
        List<JduSongCarouselCategory> categories = [];
        JduSongCarouselCategory[] versionCategories =
            CreateOriginalVersionCategories(songs, client, actionList);
        int currentGameIndex = Array.FindIndex(versionCategories, category =>
            category.Title == GetVersionTitle(client.GameYear));
        if (currentGameIndex >= 0)
        {
            JduSongCarouselCategory currentGameCategory = versionCategories[currentGameIndex];
            if (client.GameYear == 2018 && pageName == "party")
                currentGameCategory = AppendShuffle(currentGameCategory);
            categories.Add(currentGameCategory);

            // The carousel wraps vertically. Put older games directly below the
            // current game, and leave newer games in descending order at the end so
            // the nearest newer game wraps directly above it.
            categories.AddRange(versionCategories.Skip(currentGameIndex + 1));
        }

        categories.AddRange(ClassicJduGameModeCatalog
            .GetFor(client, pageName, actionList)
            .Select(row => CreateBuiltInCategory(row, client, allSongs)));
        categories.AddRange(CreateAlphabetCategories(songs, actionList));
        if (currentGameIndex > 0)
            categories.AddRange(versionCategories.Take(currentGameIndex));
        return CreateResponse([.. categories]);
    }

    private static JduSongCarouselCategory[] CreateAlphabetCategories(
        JustDanceSongDBEntry[] songs,
        string actionList = "partyMap")
        =>
        [
            .. CarouselPartitioner.Create(songs, GetSortName)
                .Select(partition => CreateCategory(partition.Title, partition.Items, actionList))
        ];

    private static JduSongCarouselCategory[] CreateOriginalVersionCategories(
        JustDanceSongDBEntry[] songs,
        ClassicJduClientProfile client,
        string actionList = "partyMap")
    {
        Dictionary<int, HashSet<string>> mapsByVersion = songs
            .GroupBy(song => song.OriginalJDVersion)
            .ToDictionary(
                group => group.Key,
                group => group.Select(song => song.MapName).ToHashSet(StringComparer.OrdinalIgnoreCase));

        if (!mapsByVersion.TryGetValue(client.GameYear, out HashSet<string>? currentGameMaps))
        {
            currentGameMaps = [with(StringComparer.OrdinalIgnoreCase)];
            mapsByVersion[client.GameYear] = currentGameMaps;
        }

        currentGameMaps.UnionWith(ClassicJduBaseCatalog.GetMainMaps(client));

        return
        [
            .. mapsByVersion
                .OrderBy(group => group.Key <= 0 ? 1 : 0)
                .ThenByDescending(group => group.Key)
                .Select(group => CreateCategory(
                    GetVersionTitle(group.Key),
                    group.Value.Order(StringComparer.OrdinalIgnoreCase),
                    actionList))
        ];
    }

    private static JduSongCarouselContentResponse CreateKidsResponse()
    {
        JduSongCarouselCategory kids = CreateCategory(
            "Kids",
            ClassicJduGameModeCatalog.Jdu2018Kids,
            "kidsMap",
            "grp_kids_row",
            "kids_grp_cover");
        return CreateResponse([AppendShuffle(kids, "kids_grp_cover_shuffle")]);
    }

    private JustDanceSongDBEntry[] GetSubscriptionSongs(ClassicJduClientProfile client)
    {
        HashSet<string> baseMaps = new(
            ClassicJduBaseCatalog.GetMaps(client),
            StringComparer.OrdinalIgnoreCase);
        baseMaps.UnionWith(ClassicJduGameModeCatalog.GetOfflineMapNames(client));

        return
        [
            .. GetAllSongs()
                .Where(song => !string.IsNullOrWhiteSpace(song.MapName) && !baseMaps.Contains(song.MapName))
        ];
    }

    private JustDanceSongDBEntry[] GetAllSongs()
        =>
        [
            .. JduCatalogSelection.FromClassic(mapService.Maps.Values.Select(map => map.Song))
                .Where(song => !string.IsNullOrWhiteSpace(song.MapName))
                .GroupBy(song => song.MapName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(GetSortName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(song => song.MapName, StringComparer.OrdinalIgnoreCase)
        ];

    private static JustDanceSongDBEntry[] FilterSongs(
        JustDanceSongDBEntry[] songs,
        JduCarouselRequest? request)
    {
        if (request is null)
            return songs;

        IEnumerable<JustDanceSongDBEntry> filtered = songs;
        if (!string.IsNullOrWhiteSpace(request.SearchString))
        {
            string query = request.SearchString.Trim();
            filtered = filtered.Where(song =>
                song.MapName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || song.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || song.Artist.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (request.SearchTags is { Length: > 0 })
        {
            filtered = filtered.Where(song => request.SearchTags.All(searchTag =>
                song.Tags.Any(tag => string.Equals(tag, searchTag, StringComparison.OrdinalIgnoreCase))));
        }

        return [.. filtered];
    }

    private static JduSongCarouselCategory CreateBuiltInCategory(
        ClassicJduCarouselRow row,
        ClassicJduClientProfile client,
        JustDanceSongDBEntry[] allSongs)
    {
        JduSongCarouselCategory category = row.Source switch
        {
            ClassicJduCarouselRowSource.CoachCount => CreateCategory(
                row.Title,
                GetCoachCountMaps(client, allSongs, row.CoachCounts),
                row.ActionList,
                row.Isc),
            ClassicJduCarouselRowSource.MapList => CreateCategory(
                row.Title,
                row.MapNames,
                row.ActionList,
                row.Isc),
            ClassicJduCarouselRowSource.Favorites => CreateFavoritesCategory(row),
            ClassicJduCarouselRowSource.Playlist => CreatePlaylistCategory(row),
            ClassicJduCarouselRowSource.Episodes => CreateEpisodeCategory(row),
            _ => throw new ArgumentOutOfRangeException(nameof(row), row.Source, "Unknown classic carousel row source.")
        };

        return row.AppendShuffle ? AppendShuffle(category) : category;
    }

    private static IEnumerable<string> GetCoachCountMaps(
        ClassicJduClientProfile client,
        IEnumerable<JustDanceSongDBEntry> songs,
        IReadOnlyList<int> coachCounts)
    {
        IEnumerable<string> mapNames = songs
            .Where(song => coachCounts.Contains(song.CoachCount is >= 1 and <= 4 ? song.CoachCount : 1))
            .Select(song => song.MapName);

        // WakaWakaALT is present in both JD2018 dumps but is currently the only
        // normal disc song absent from our server map collection.
        if (client.GameYear == 2018 && coachCounts.Contains(4))
            mapNames = mapNames.Append("WakaWakaALT");

        return mapNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);
    }

    private static JduSongCarouselCategory CreateFavoritesCategory(ClassicJduCarouselRow row)
        => new()
        {
            Title = row.Title,
            Isc = row.Isc,
            Items =
            [
                new JduSongCarouselItem
                {
                    Isc = "grp_favorite",
                    Components = [],
                    ActionList = "favoriteActions"
                }
            ]
        };

    private static JduSongCarouselCategory CreatePlaylistCategory(ClassicJduCarouselRow row)
        => new()
        {
            Title = row.Title,
            Isc = row.Isc,
            Items =
            [
                new JduSongCarouselItem
                {
                    Isc = "grp_playlist_add",
                    Components = [new JduCarouselPlaylistItemComponent()],
                    ActionList = "new-playlist"
                }
            ]
        };

    private static JduSongCarouselCategory CreateEpisodeCategory(ClassicJduCarouselRow row)
    {
        List<JduSongCarouselItem> items = [];
        for (int episode = 1; episode <= 8; episode++)
        {
            items.Add(new JduSongCarouselItem
            {
                Isc = "dancemachine_item",
                Components = [new JduCarouselEpisodeComponent { Episode = $"JDM_Episode{episode}" }],
                ActionList = episode == 1 ? "playEpisodeActionList" : "lockedEpisodeActionList"
            });
        }

        items.Add(new JduSongCarouselItem
        {
            Isc = "dancemachine_item_random",
            Components = [new JduCarouselEpisodeComponent { Episode = "JD_DMEpisodeRandom" }],
            ActionList = "lockedEpisodeActionList"
        });

        return new JduSongCarouselCategory
        {
            Title = row.Title,
            Isc = row.Isc,
            Items = [.. items]
        };
    }

    private static JduSongCarouselCategory AppendShuffle(
        JduSongCarouselCategory category,
        string itemScene = "grp_shuffle")
        => category with
        {
            Items =
            [
                .. category.Items,
                new JduSongCarouselItem
                {
                    Isc = itemScene,
                    Components = [new JduCarouselShuffleComponent()],
                    ActionList = "NonStop",
                    EndPos = 3
                }
            ]
        };

    private static JduSongCarouselCategory CreateCategory(
        string title,
        IEnumerable<JustDanceSongDBEntry> songs,
        string actionList,
        string isc = "grp_row",
        string itemScene = "grp_cover")
        => CreateCategory(title, songs.Select(song => song.MapName), actionList, isc, itemScene);

    private static JduSongCarouselCategory CreateCategory(
        string title,
        IEnumerable<string> mapNames,
        string actionList,
        string isc = "grp_row",
        string itemScene = "grp_cover")
        => new()
        {
            Title = title,
            Isc = isc,
            Items =
            [
                .. mapNames.Select(mapName => new JduSongCarouselItem
                {
                    Isc = itemScene,
                    Components = [new JduCarouselSongComponent { MapName = mapName }],
                    ActionList = actionList
                })
            ]
        };

    private static JduSongCarouselContentResponse CreateResponse(
        JduSongCarouselCategory[] categories)
    {
        JduSongCarouselCategory[] nonEmptyCategories =
            [.. categories.Where(category => category.Items.Length > 0)];
        HashSet<string> referencedActionLists = nonEmptyCategories
            .SelectMany(category => category.Items)
            .Select(item => item.ActionList)
            .ToHashSet(StringComparer.Ordinal);

        return new()
        {
            Categories = nonEmptyCategories,
            ActionLists = ClassicJduCarouselActions.All
                .Where(actionList => referencedActionLists.Contains(actionList.Key))
                .ToDictionary(
                    actionList => actionList.Key,
                    actionList => actionList.Value,
                    StringComparer.Ordinal),
        };
    }

    private static string GetSortName(JustDanceSongDBEntry song)
        => string.IsNullOrWhiteSpace(song.Title) ? song.MapName.Trim() : song.Title.Trim();

    private static string GetVersionTitle(int version)
        => version switch
        {
            <= 0 => "Misc versions",
            123 => "Just Dance Kids",
            404 => "Unplayable songs",
            4514 => "Just Dance Asia",
            4884 => "ABBA: You Can Dance",
            _ => $"Just Dance {version}"
        };

}
