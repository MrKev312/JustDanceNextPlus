using JustDanceServer.Configuration;

namespace JustDanceServer.Services;

public enum ClassicJduCarouselRowSource
{
	CoachCount,
	MapList,
	Favorites,
	Playlist,
	Episodes
}

public sealed record ClassicJduCarouselRow(
	string Title,
	string Isc,
	ClassicJduCarouselRowSource Source,
	string ActionList,
	IReadOnlyList<int> CoachCounts,
	IReadOnlyList<string> MapNames,
	bool AppendShuffle = false);

public static class ClassicJduGameModeCatalog
{
	private static readonly string[] Jdu2016Showtime =
	[
		"AboutThatBassSHI", "BornThisWaySHI", "HeyMamaSHI", "IGotAFeelingSHI", "LightsSHI",
		"ThisIsHowSHI", "UptownFunkSHI", "WantToWantMeSHI", "WhenTheRainSHI", "YoureTheOneSHI"
	];

	private static readonly string[] Jdu2016Mashups =
	[
		"AboutThatBassMU", "AlbatraozMU", "AnimalsMU", "BlameMU", "BornThisWayMU", "BoysBoysMU",
		"ChiwawaMU", "CircusMU", "ElectroMamboMU", "FancyMU", "HangoverMU", "IGotAFeelingMU",
		"LetsGrooveMU", "LevanPolkkaMU", "LightsMU", "RabiosaMU", "StadiumFlowMU",
		"StuckOnAFeelingMU", "TeacherMU", "UptownFunkMU"
	];

	private static readonly string[] Jdu2016CommunityRemixes = ["UptownFunkCMU"];

	private static readonly string[] Jdu2016PartyMaster =
	[
		"AboutThatBassPM", "BornThisWayALTPM", "TeacherPM", "UptownFunkPM", "WantToWantMePM"
	];

	private static readonly string[] Jdu2017Mashups = ["ScreamNShoutMU"];

	private static readonly string[] Jdu2018DoubleRumble =
	[
		"SwitchChef", "SwitchHandyman", "SwitchMariachi", "SwitchSport", "SwitchWitch"
	];

	private static readonly string[] Jdu2018Fitness =
	[
		"24K", "AnotherOne", "AnotherOneALT", "Dharma", "Footloose", "Instruction",
		"MakeItJingle", "NewFace", "SayonaraRetake", "SideToALT"
	];

	public static IReadOnlyList<string> Jdu2018Kids { get; } =
	[
		"MagicHalloweenKids", "HowFar", "FearlessPirateKids", "FunkyRobotKids", "HappyFarmKids",
		"PixieLandKids", "Blue", "WakaWakaKids", "FootlooseKids", "BeepBeep", "MissAmazingKids",
		"DaddyCool"
	];

	public static IReadOnlyList<ClassicJduCarouselRow> GetFor(
		ClassicJduClientProfile client,
		string pageName,
		string mapActionList)
	{
		string page = pageName.ToLowerInvariant();
		if (client.GameYear == 2016)
			return Create2016Rows(client, page, mapActionList);
		if (client.GameYear == 2017)
			return Create2017Rows(mapActionList);
		if (client.GameYear == 2018 && page == "party")
			return Create2018PartyRows(client, mapActionList);

		// The JD2018 party-coop and sweat rules in both dumps deliberately retain
		// the JD2017 carousel layout.
		return Create2017StyleRows(mapActionList);
	}

	public static IReadOnlySet<string> GetOfflineMapNames(ClassicJduClientProfile client)
	{
		HashSet<string> maps = [with(StringComparer.OrdinalIgnoreCase)];
		if (client.GameYear == 2016)
		{
			maps.UnionWith(Jdu2016Showtime);
			maps.UnionWith(Jdu2016Mashups);
			maps.UnionWith(Jdu2016CommunityRemixes);
			if (client.AssetPlatform == "WIIU")
				maps.UnionWith(Jdu2016PartyMaster);
		}
		else if (client.GameYear == 2017)
		{
			maps.UnionWith(Jdu2017Mashups);
		}
		else if (client.GameYear == 2018)
		{
			maps.UnionWith(Jdu2018DoubleRumble);
			maps.UnionWith(Jdu2018Kids);
		}

		return maps;
	}

	private static IReadOnlyList<ClassicJduCarouselRow> Create2016Rows(
		ClassicJduClientProfile client,
		string page,
		string mapActionList)
	{
		List<ClassicJduCarouselRow> rows =
		[
			CoachRow("SOLO", 1, mapActionList),
			CoachRow("DUO", 2, mapActionList),
			CoachRow("TRIO", 3, mapActionList),
			CoachRow("QUATRO", 4, mapActionList)
		];

		if (page != "sweat")
			rows.Add(MapRow("SHOWTIME", "grp_row_wide", "showtimeMap", Jdu2016Showtime));

		rows.Add(MapRow("MASHUP", "grp_row", mapActionList, Jdu2016Mashups));

		if (page != "sweat")
		{
			rows.Add(MapRow(
				"STAR REMIX / COMMUNITY REMIX",
				"grp_row",
				page == "partycoop" ? "partyMapCoopCMUSR" : "partyMapCMUSR",
				Jdu2016CommunityRemixes));
			// The rule file also names an internal PC target, but JD2016 never
			// shipped on PC. Party Master is a retail Wii U-only row.
			if (client.AssetPlatform == "WIIU")
				rows.Add(MapRow("PARTY MASTER", "grp_row", "Party-Master", Jdu2016PartyMaster));
		}

		return rows;
	}

	private static IReadOnlyList<ClassicJduCarouselRow> Create2017Rows(string mapActionList)
		=> Create2017StyleRows(mapActionList);

	private static IReadOnlyList<ClassicJduCarouselRow> Create2017StyleRows(string mapActionList)
	{
		List<ClassicJduCarouselRow> rows =
		[
			SpecialRow("Favorites", "grp_row_favorite", ClassicJduCarouselRowSource.Favorites),
			CoachRow("Solo", 1, mapActionList),
			CoachRow("Duet", 2, mapActionList),
			CoachRow("Trio", 3, mapActionList),
			CoachRow("Quatro", 4, mapActionList)
		];

		rows.Add(MapRow("Mashup", "grp_row", mapActionList, Jdu2017Mashups));
		return rows;
	}

	private static IReadOnlyList<ClassicJduCarouselRow> Create2018PartyRows(
		ClassicJduClientProfile client,
		string mapActionList)
	{
		List<ClassicJduCarouselRow> rows = [];
		if (client.AssetPlatform == "NX")
			rows.Add(MapRow("Double Rumble", "grp_row", "HdRumbleMap", Jdu2018DoubleRumble));

		rows.Add(SpecialRow("Episodes", "grp_row", ClassicJduCarouselRowSource.Episodes));
		rows.Add(SpecialRow("Favorites", "grp_row_favorite", ClassicJduCarouselRowSource.Favorites));
		rows.Add(SpecialRow("Playlist", "grp_row_playlist", ClassicJduCarouselRowSource.Playlist));
		rows.Add(MapRow("Fitness", "grp_row_fitness", mapActionList, Jdu2018Fitness, appendShuffle: true));
		rows.Add(CoachRow("Solo", 1, mapActionList, appendShuffle: true));
		rows.Add(CoachRow("Duet", 2, mapActionList, appendShuffle: true));
		rows.Add(CoachRow("Trio", 3, mapActionList, appendShuffle: true));
		rows.Add(CoachRow("Quatro", 4, mapActionList, appendShuffle: true));
		return rows;
	}

	private static ClassicJduCarouselRow CoachRow(
		string title,
		int coachCount,
		string actionList,
		bool appendShuffle = false)
		=> CoachRow(title, [coachCount], actionList, appendShuffle);

	private static ClassicJduCarouselRow CoachRow(
		string title,
		IReadOnlyList<int> coachCounts,
		string actionList,
		bool appendShuffle = false)
		=> new(
			title,
			"grp_row",
			ClassicJduCarouselRowSource.CoachCount,
			actionList,
			coachCounts,
			[],
			appendShuffle);

	private static ClassicJduCarouselRow MapRow(
		string title,
		string isc,
		string actionList,
		IReadOnlyList<string> mapNames,
		bool appendShuffle = false)
		=> new(
			title,
			isc,
			ClassicJduCarouselRowSource.MapList,
			actionList,
			[],
			mapNames,
			appendShuffle);

	private static ClassicJduCarouselRow SpecialRow(
		string title,
		string isc,
		ClassicJduCarouselRowSource source)
		=> new(title, isc, source, string.Empty, [], []);
}
