using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public class SongDBTypeSet(string url)
{
	public string SongdbUrl { get; set; } = $"https://{url}/songdb/songDB";
	public Songoffers SongOffers { get; set; } = new();
}

public class Songoffers
{
	public HashSet<Guid> FreeSongs { get; set; } = [];
	public HashSet<Guid> HiddenSongs { get; set; } = [];
	public HashSet<Guid> LocalTracks { get; set; } = [];
	public Dictionary<string, Pack> Claims { get; set; } = [];
	public HashSet<Guid> DownloadableSongs { get; set; } = [];
}

public class Pack
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? DescriptionLocId { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? AllowSharing { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? FreeTrialDurationMinutes { get; set; }
	public HashSet<Guid> RewardIds { get; set; } = [];
	public HashSet<Guid> SongIds { get; set; } = [];
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public HashSet<string>? SongPackIds { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? UnlocksFullVersion { get; set; }
}
