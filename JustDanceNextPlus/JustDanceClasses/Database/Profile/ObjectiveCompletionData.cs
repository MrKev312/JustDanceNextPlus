using System.ComponentModel.DataAnnotations;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

public class ObjectiveCompletionData
{
	[Key]
	public Guid ObjectiveId { get; set; }
	public DateTime CompletedAt { get; set; }
}
