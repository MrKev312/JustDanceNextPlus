using System.ComponentModel.DataAnnotations;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

public class CompletedTask
{
	[Key]
	public Guid TaskId { get; set; }
	public DateTime CompletedAt { get; set; }
}
