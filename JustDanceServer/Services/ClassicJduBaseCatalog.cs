using JustDanceServer.Configuration;

namespace JustDanceServer.Services;

public static class ClassicJduBaseCatalog
{
    private static readonly IReadOnlyDictionary<int, string[]> MapsByYear =
        new Dictionary<int, string[]>
        {
            [2016] =
            [
                "AboutThatBass", "AboutThatBassALT", "Albatraoz", "AngryBirds", "Animals", "AnimalsALT",
                "Blame", "BollywoodXmas", "BornThisWay", "BornThisWayALT", "BoysBoys", "Cheerleader", "Chiwawa", "Circus",
                "CircusALT", "Coolos", "CopaCabana", "ElectroMambo", "ElectroTribalDLC", "Fancy", "FancyALT",
                "FeelSoRight", "Fun", "Gibberish", "Hangover", "Heartbeat", "HeyMama", "HeyMamaALT", "HitTheRoad",
                "HitTheRoadALT", "IGotAFeeling", "IGotAFeelingALT", "JuntoATi", "KaboomPow", "KungFunk", "LetsGroove",
                "LevanPolkka", "Lights", "NoControl", "OnMyMind", "Rabiosa", "RabiosaALT", "SaintPatrick", "Sirtaki",
                "SpectronizerQUAT", "StadiumFlow", "Stargate", "StuckOnAFeeling", "Teacher", "TeacherALT", "TheChoice",
                "TheseBoots", "ThisIsHow", "ThisIsHowALT", "UnderTheSea", "UptownFunk", "UptownFunkALT",
                "Ulibayssia", "WantToWantMe", "WantToWantMeALT", "WhenTheRain", "WilliamTell", "YouNeverCan", "YoureTheOne", "Zagreb"
            ],
            [2017] =
            [
                "AllAboutUs", "Bailar", "Bang", "Bicicleta", "Bonbon", "CakeByTheOcean", "CakeByTheOceanALT",
                "CantFeelMyFace", "CheapThrills", "CheapThrillsALT", "ColaSong", "ColaSongALT", "Daddy", "DaddyALT",
                "DontStopMe", "DontStopMeALT", "DragosteaDinTei", "ElectroMambo", "ElTiki", "ElTikiALT",
                "GhostInTheKeys", "Groove", "Hips", "HipsALT", "ILoveRock", "IntoYou", "KungFunk", "LastChristmas",
                "LeanOn", "LeanOnALT", "Leila", "LikeIWould", "LittleSwing", "NaeNae", "NaeNaeALT", "Oishii",
                "PoPiPo", "Radical", "RadicalALT", "RedMangoose", "RunTheNight", "Samba", "ScreamNShout",
                "ScreamNShoutALT", "September", "SeptemberALT", "SingleLadies", "Sorry", "SorryALT", "TeDominar",
                "TicoTico", "Titanium", "WhatIsLove", "WhatIsLoveALT", "WhereverIGo", "WilliamTell", "WorthIt", "WorthItALT"
            ],
            [2018] =
            [
                "24K", "24KALT", "8BitRetake", "AllYouGotta", "AnotherOne", "AnotherOneALT", "Automaton",
                "AutomatonALT", "BadLiar", "BeepBeep", "BlowYourMind", "Blue", "BubblePop", "BubblePopALT",
                "Carmen", "Chantaje", "ChantajeALT", "Copperhead", "Cottonmouth", "DaddyCool", "Despacito",
                "DespacitoALT", "Dharma", "DharmaALT", "Diggy", "FearlessPirateKids", "Footloose", "FootlooseKids",
                "FunkyRobotKids", "GotThat", "HappyFarmKids", "HowFar", "Instruction", "ItsyBitsyRetake", "JohnW",
                "JohnWALT", "KeepOn", "KissingStrangers", "KissingStrangersALT", "LoveWard", "MagicHalloweenKids",
                "MakeItJingle", "MissAmazingKids", "NaughtyGirl", "NewFace", "PixieLandKids", "RiskyBusiness",
                "Rockabye", "SayonaraRetake", "ShapeOfYou", "SideTo", "SideToALT", "Sidewinder", "SlumberParty",
                "SugarDance", "SwishSwish", "TumBum", "TumBumALT", "WakaWaka", "WakaWakaALT", "WakaWakaKids"
            ]
        };

    private static readonly IReadOnlySet<string> Jdu2016UplayMaps = new HashSet<string>(
        ["BollywoodXmas", "ElectroTribalDLC", "FeelSoRight", "OnMyMind", "Sirtaki", "SpectronizerQUAT", "Zagreb"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlySet<string> Jdu2017UplayMaps = new HashSet<string>(
        ["ElectroMambo", "KungFunk", "WilliamTell"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly string[] Jdu2017SwitchMaps =
    [
        "BedtimeStoryCHN", "BigDreamerCHN", "Imya505", "KaraokeForeverCHN", "LegSongCHN", "Natoo"
    ];

    public static IReadOnlyList<string> GetMaps(int gameYear)
        => MapsByYear.TryGetValue(gameYear, out string[]? maps) ? maps : [];

    public static IReadOnlyList<string> GetWdfMaps(int gameYear)
    {
        IReadOnlySet<string>? serverBackedMaps = gameYear switch
        {
            2016 => Jdu2016UplayMaps,
            2017 => Jdu2017UplayMaps,
            _ => null
        };

        return serverBackedMaps is null
            ? GetMaps(gameYear)
            : [.. GetMaps(gameYear).Where(mapName => !serverBackedMaps.Contains(mapName))];
    }

    public static IReadOnlyList<string> GetMaps(ClassicJduClientProfile client)
    {
        IEnumerable<string> maps = GetWdfMaps(client.GameYear);
        if (client.GameYear == 2017 && client.AssetPlatform == "NX")
            maps = maps.Concat(Jdu2017SwitchMaps);

        // Uplay is gone and these maps are hosted by the server like every other
        // subscription song. Do not let their disc entries shadow the server copy.
        return [.. maps];
    }

    public static IReadOnlyList<string> GetMainMaps(ClassicJduClientProfile client)
        =>
        [
            .. GetMaps(client).Where(mapName =>
                client.GameYear != 2018 || !ClassicJduGameModeCatalog.Jdu2018Kids.Contains(mapName))
        ];
}
