using System.ComponentModel.DataAnnotations;

namespace JustDanceServer.JustDanceClasses.Database.Profile;

public class ObjectiveCompletionData
{
    [Key]
    public Guid ObjectiveId { get; set; }
    public DateTime CompletedAt { get; set; }
}
