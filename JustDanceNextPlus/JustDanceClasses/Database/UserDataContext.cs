using JustDanceNextPlus.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public class UserDataContext(DbContextOptions<UserDataContext> options) : DbContext(options)
{
	public DbSet<Profile.Profile> Profiles { get; set; }
	public DbSet<MapStats> HighScores { get; set; }

	//public DbSet<MapHistory> MapHistories { get; set; }
	public DbSet<RunningTask> RunningTasks { get; set; }
	public DbSet<CompletedTask> CompletedTasks { get; set; }
	public DbSet<ObjectiveCompletionData> ObjectiveCompletionData { get; set; }
	public DbSet<BossModeStats> BossStats { get; set; }
}
