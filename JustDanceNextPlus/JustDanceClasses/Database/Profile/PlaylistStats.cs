using Microsoft.EntityFrameworkCore;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

[PrimaryKey("PlaylistId", "ProfileId")]
public class PlaylistStats
{
	[JsonIgnore]
	public Guid PlaylistId { get; set; }
	[JsonIgnore]
	public Guid ProfileId { get; set; }

	public int HighScore { get; set; }
	public int PlayCount { get; set; }
	public string Platform { get; set; } = "Unknown";
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	public Dictionary<Guid, int> HighScorePerMap { get; set; } = [];
}
