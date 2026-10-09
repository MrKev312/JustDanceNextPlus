using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Utilities;

namespace JustDanceServer.Services;

public static class MenuProjection
{
	public static ActivityPageResponse AddRows(ActivityPageResponse baseline, IReadOnlyList<MenuRow> rows, IMapService maps)
	{
		var categories = baseline.Categories.ToBuilder();
		var modifiers = baseline.CategoryModifiers.ToBuilder();
		for (int index = 0; index < rows.Count; index++)
		{
			MenuRow row = rows[index];
			if (categories.ContainsKey(row.Id))
				throw new InvalidOperationException($"Menu row '{row.Key}' conflicts with an existing activity category.");
			categories.Add(row.Id, new CarouselCategory
			{
				CategoryName = row.Title,
				TitleId = row.TitleTag ?? throw new InvalidOperationException($"Menu row '{row.Key}' has no JDNext text alias."),
				ItemList = [.. row.MapNames.Select(name => new MapTag
				{
					Guid = maps.GetSongId(name) ?? throw new InvalidOperationException($"Unknown menu map '{name}'.")
				})]
			});
			modifiers.Add(new PositionModifier("carousel", row.Id, baseline.Categories.Count + index));
		}

		return baseline with { Categories = categories.ToImmutable(), CategoryModifiers = modifiers.ToImmutable() };
	}

	public static JduSongCarouselContentResponse AddRows(JduSongCarouselContentResponse baseline, IReadOnlyList<MenuRow> rows)
	{
		Dictionary<string, JduCarouselActionList> actions = [with(baseline.ActionLists)];
		if (rows.Count > 0)
			actions["partyMap"] = ClassicJduCarouselActions.All["partyMap"];
		return baseline with
		{
			Categories = [.. baseline.Categories, .. rows.Select(row => new JduSongCarouselCategory
			{
				Title = row.Title,
				Items = [.. row.MapNames.Select(name => new JduSongCarouselItem
				{
					Isc = "grp_cover", ActionList = "partyMap",
					Components = [new JduCarouselSongComponent { MapName = name }]
				})]
			})],
			ActionLists = actions
		};
	}

	public static JduPlaylistDbResponse AddPlaylists(JduPlaylistDbResponse baseline, IReadOnlyList<MenuRow> rows, int? length)
	{
		Dictionary<string, JduPlaylist> playlists = new(baseline.Db, StringComparer.OrdinalIgnoreCase);
		foreach (MenuRow row in rows)
		{
			playlists.Add(row.Key, new JduPlaylist
			{
				Type = "recommended", Title = row.Title, CoverURL = row.CoverUrl,
				Maps = length is > 0 ? [.. row.MapNames.Take(length.Value)] : [.. row.MapNames],
				FixedMapOrder = true
			});
		}

		return baseline with { Db = playlists };
	}

	public static JduCarouselContentResponse AddPlaylists(JduCarouselContentResponse baseline, IReadOnlyList<MenuRow> rows)
		=> baseline with
		{
			Categories = [.. baseline.Categories, .. rows.Select(row => new JduCarouselCategory
			{
				Title = row.Title,
				Items = [new JduCarouselItem
				{
					Components = [new JduCarouselPlaylistComponent { PlaylistID = row.Key }]
				}]
			})]
		};
}
