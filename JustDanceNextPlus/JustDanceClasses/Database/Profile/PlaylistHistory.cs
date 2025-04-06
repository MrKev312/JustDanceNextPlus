using System.ComponentModel.DataAnnotations;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

public class PlaylistHistory
{
	[Key]
	public Guid Name { get; set; } = Guid.Empty;
	public int Score { get; set; }
	public string Platform { get; set; } = "uplay";
	public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
