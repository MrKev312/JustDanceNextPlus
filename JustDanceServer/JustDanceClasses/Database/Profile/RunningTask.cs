using System.ComponentModel.DataAnnotations;

namespace JustDanceServer.JustDanceClasses.Database.Profile;

public class RunningTask
{
    [Key]
    public Guid TaskId { get; set; }
    public int CurrentLevel { get; set; }
    public int Type { get; set; }
    public int StepsDone { get; set; }
}
