using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

public class BossModeStats
{
	[Key]
	[JsonIgnore]
	public Guid BossStatsId { get; set; }

	public virtual BossMode BossMode { get; set; } = new();
}

public class BossMode
{
	[Key]
	[JsonIgnore]
	public Guid BossId { get; set; }
}
