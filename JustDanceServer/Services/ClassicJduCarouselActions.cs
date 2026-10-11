using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public static class ClassicJduCarouselActions
{
    public static IReadOnlyDictionary<string, JduCarouselActionList> All { get; } =
        new Dictionary<string, JduCarouselActionList>
        {
            ["partyMap"] = CreateMapActions("family_rival", "Dance!"),
            ["partyMapCoop"] = CreateMapActions("family_coop"),
            ["sweatMap"] = CreateMapActions("CONTEXT_SWEAT"),
            ["kidsMap"] = CreateKidsMapActions(),
            ["HdRumbleMap"] = CreateHdRumbleMapActions(),
            ["NonStop"] = CreateSingleAction("start-rowPlaylist", "Play Shuffle", "SHUFFLE"),
            ["favoriteActions"] = CreateSingleAction(string.Empty, string.Empty, "favorite"),
            ["new-playlist"] = CreateSingleAction("edit-playlist", "Create Playlist", "PLAYLIST_ADD", "playlist"),
            ["playEpisodeActionList"] = CreateEpisodeActions("default"),
            ["playEpisodeRandomActionList"] = CreateEpisodeActions("random"),
            ["lockedEpisodeActionList"] = CreateSingleAction("episode-locked", "Locked", "song_locked_gasha"),
            ["showtimeMap"] = CreateShowtimeActions(),
            ["partyMapCMUSR"] = CreateMapActions("family_rival"),
            ["partyMapCoopCMUSR"] = CreateMapActions("family_coop"),
            ["Party-Master"] = CreateSingleAction("play-song", "Party-Master", "SONG_PARTYMASTER")
        };

    private static JduCarouselActionList CreateMapActions(
        string bannerContext,
        string playTitle = "Play") => new()
        {
            Actions =
        [
            new JduCarouselAction
            {
                BannerContext = bannerContext,
                BannerTheme = "DEFAULT",
                BannerType = "song",
                Title = playTitle,
                Type = "play-song"
            },
            new JduCarouselAction
            {
                BannerContext = bannerContext,
                BannerTheme = "DEFAULT",
                BannerType = "song_licensing",
                Title = "Credits",
                Type = "credits"
            }
        ]
        };

    private static JduCarouselActionList CreateKidsMapActions() => new()
    {
        Actions =
        [
            new JduCarouselAction
            {
                BannerContext = "kids",
                BannerTheme = "DEFAULT",
                BannerType = "song",
                Title = "Play",
                Type = "play-song"
            }
        ]
    };

    private static JduCarouselActionList CreateHdRumbleMapActions() => new()
    {
        Actions =
        [
            new JduCarouselAction
            {
                BannerContext = "default",
                BannerTheme = "DEFAULT",
                BannerType = "hd_rumble",
                Title = "Play",
                Type = "play-song"
            },
            new JduCarouselAction
            {
                BannerContext = "default",
                BannerTheme = "DEFAULT",
                BannerType = "song_leaderboard",
                Title = "Leaderboard",
                Type = "leaderboard"
            },
            new JduCarouselAction
            {
                BannerContext = "default",
                BannerTheme = "DEFAULT",
                BannerType = "song_licensing",
                Title = "Credits",
                Type = "credits"
            }
        ]
    };

    private static JduCarouselActionList CreateSingleAction(
        string type,
        string title,
        string bannerType,
        string itemType = "map") => new()
        {
            ItemType = itemType,
            Actions =
        [
            new JduCarouselAction
            {
                BannerContext = "default",
                BannerTheme = "DEFAULT",
                BannerType = bannerType,
                Title = title,
                Type = type
            }
        ]
        };

    private static JduCarouselActionList CreateEpisodeActions(string bannerContext) => new()
    {
        ItemType = "dancemachine",
        Actions =
        [
            new JduCarouselAction
            {
                BannerContext = bannerContext,
                BannerTheme = "default",
                BannerType = "dancemachine",
                Title = "play",
                Type = "play-episode"
            }
        ]
    };

    private static JduCarouselActionList CreateShowtimeActions() => new()
    {
        Actions =
        [
            new JduCarouselAction
            {
                BannerContext = "default",
                BannerType = "SONG_SHOWTIME",
                Title = "Play",
                Type = "play-song"
            },
            new JduCarouselAction
            {
                BannerType = "song_licensing",
                Title = "Credits",
                Type = "credits"
            }
        ]
    };
}
