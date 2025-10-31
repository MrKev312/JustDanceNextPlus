using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

public class Profile
{
    [Key]
	public Guid Id { get; set; } = Guid.Empty;
	[JsonIgnore]
	public string Ticket { get; set; } = "";

	[JsonIgnore]
	public virtual DancerCard Dancercard { get; set; } = new();

	public int CurrentXP { get; set; } = 1;
	public int CurrentLevel { get; set; } = 1;
	public int PrestigeGrade { get; set; } = 0;

	// Ignore in the database
	[NotMapped]
	public MapStat MapStat { get; set; } = new();
	[NotMapped]
	public Ownership Ownership { get; set; } = new();
	[NotMapped]
	public Dictionary<Guid, MapStats> MapStats { get; set; } = [];
	[NotMapped]
	public Dictionary<Guid, PlaylistStats> PlaylistStats { get; set; } = [];
	[NotMapped]
	public List<MapHistory> MapHistory { get; set; } = [];
	[NotMapped]
	public List<PlaylistHistory> PlaylistHistory { get; set; } = [];
	[NotMapped]
	public Dictionary<Guid, RunningTask> RunningTasks { get; set; } = [];
	[NotMapped]
	public Dictionary<Guid, CompletedTask> CompletedTasks { get; set; } = [];
	[NotMapped]
	public Dictionary<Guid, ObjectiveCompletionData> ObjectiveCompletionData { get; set; } = [];
	[NotMapped]
	public BossModeStats BossModeStats { get; set; } = new();
}

public class MapStat
{
	public string Platform { get; set; } = "switch";
}

public class Ownership
{
	public IList<string> Claims { get; set; } = [];
}