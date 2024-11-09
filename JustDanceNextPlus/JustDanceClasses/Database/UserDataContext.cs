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

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<MapStats>(builder =>
		{
			builder.HasKey(m => new { m.MapId, m.ProfileId });

			builder.OwnsOne(m => m.HighScorePerformance, hs =>
			{
				hs.OwnsOne(h => h.Moves);
			});

			builder.OwnsOne(m => m.GameModeStats, gs =>
			{
				gs.OwnsOne(g => g.Challenge);
			});
		});
	}
}
