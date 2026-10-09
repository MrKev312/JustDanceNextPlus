using System.Text.Json.Serialization;

namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record ClassicJduConfigurationResponse
{
    public required ClassicJduConfiguration Configuration { get; init; }
}

public sealed record ClassicJduConfiguration
{
    public required ClassicJduPlatformConfiguration PlatformConfig { get; init; }
    public ClassicJduFeatureSwitch[] FeaturesSwitches { get; init; } = [];
    public ClassicJduGatewayResource[] GatewayResources { get; init; } = [];
}

public sealed record ClassicJduFeatureSwitch
{
    public required string Name { get; init; }
    public bool Value { get; init; } = true;
}

public sealed record ClassicJduPlatformConfiguration
{
    public required Guid ApplicationId { get; init; }
    public required Guid SpaceId { get; init; }
    public required string Platform { get; init; }
    public string Environment { get; init; } = "PROD";
    public string UplayGameCode { get; init; } = string.Empty;
}

public sealed record ClassicJduGatewayResource
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required int Version { get; init; }
}

public sealed record ClassicJduPopulation
{
    public required string Name { get; init; }
    public required ClassicJduServerInfo Obj { get; init; }
    public required string Subject { get; init; }
    public required Guid SpaceId { get; init; }
    public required string AssignmentTime { get; init; }
}

public sealed record ClassicJduServerInfo
{
    public string Name { get; init; } = "this is a test";
    public required string Url { get; init; }
}

public sealed record ClassicJduEntity
{
    public required Guid EntityId { get; init; }
    public required Guid SpaceId { get; init; }
    public string Type { get; init; } = "server";
    public string Name { get; init; } = "qc";
    public string[] Tags { get; init; } = [];
    public required ClassicJduServerInfo Obj { get; init; }
    public uint Revision { get; init; } = 1;
    public required string LastModified { get; init; }
}

public sealed record ClassicJduSongDatabaseEntry
{
    public string MapName { get; init; } = string.Empty;
    public string ParentMapName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public int Difficulty { get; init; }
    public int SweatDifficulty { get; init; }
    public int Mode { get; init; } = 6;
    public double MapLength { get; init; }
    public int CoachCount { get; init; } = 1;
    public string Credits { get; init; } = string.Empty;
    public bool Wdf { get; init; }
    public string AudioPreviewData { get; init; } = string.Empty;
    public string CustomTypeName { get; init; } = string.Empty;
    public string LyricsColor { get; init; } = "FFFFFFFF";
    public Dictionary<string, string> SongColors { get; init; } = [];
    public string[] Tags { get; init; } = [];
    public string[] JdmAttributes { get; init; } = [];
    public bool IsPlayable { get; init; } = true;
    public Dictionary<string, string> Assets { get; init; } = [];
    public Dictionary<string, string> Urls { get; init; } = [];
    public Dictionary<string, string> Packages { get; init; } = [];
    public int OriginalJDVersion { get; init; } = 9999;
    public int MainCoach { get; init; } = -1;
    public int LyricsType { get; init; }
    public string MapPreviewMpd { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DoubleScoringType { get; init; }

    public int Status { get; init; } = 3;
}

public sealed record ClassicJduOnlineBlockDatabase
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "OnlineBlockDb";
    public Dictionary<string, object> Blocks { get; init; } = [];
}

public sealed record JduPackageEntry
{
    public required string Url { get; init; }
    public required string Md5 { get; init; }
    public required int Version { get; init; }
    public int StorageType { get; init; }
}

public sealed record JduPackageArchive(string PackageId, string FileName, Stream Contents);

public sealed record JduPackageArchiveMetadata(string PackageId, string FileName, long Length);
