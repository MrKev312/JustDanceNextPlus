using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public class SongDBTypeSet
{
	public string SongdbUrl { get; set; } = "https://prod-next.just-dance.com/songdb/songDB";
	public Songoffers SongOffers { get; set; } = new();
}

public class Songoffers
{
	public List<Guid> FreeSongs { get; set; } = [];
	public List<Guid> HiddenSongs { get; set; } = [];
	public List<Guid> LocalTracks { get; set; } = [];
	public Dictionary<string, Pack> Claims { get; set; } = [];
	public List<Guid> DownloadableSongs { get; set; } = [];
}

public class Pack
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? DescriptionLocId { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? AllowSharing { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? FreeTrialDurationMinutes { get; set; }
	public List<Guid> RewardIds { get; set; } = [];
	public List<Guid> SongIds { get; set; } = [];
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<string>? SongPackIds { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? UnlocksFullVersion { get; set; }
}
