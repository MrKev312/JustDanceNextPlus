using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using System.Text.Json;

namespace JustDanceServer.JustDanceClasses.Database;

public class UserDataContext(DbContextOptions<UserDataContext> options) : DbContext(options)
{
    public DbSet<Profile.Profile> Profiles { get; set; }
    public DbSet<SavedDancerCard> SavedDancerCards { get; set; }
    public DbSet<PlatformIdentity> PlatformIdentities { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<MapStats> MapScores { get; set; }
    public DbSet<ClassicProfile> ClassicProfiles { get; set; }
    public DbSet<PlayerProgression> PlayerProgressions { get; set; }
    public DbSet<PlayerUnlock> PlayerUnlocks { get; set; }
    public DbSet<PlayerQuestProgress> PlayerQuestProgress { get; set; }
    public DbSet<PlayerGachaState> PlayerGachaStates { get; set; }
    public DbSet<PlayerMetadata> PlayerMetadata { get; set; }
    public DbSet<PlayerMapPlay> PlayerMapPlays { get; set; }
    public DbSet<PlayerMapFavorite> PlayerMapFavorites { get; set; }
    public DbSet<PlayerMapOrdinal> PlayerMapOrdinals { get; set; }
    //public DbSet<MapHistory> MapHistories { get; set; }
    public DbSet<PlaylistStats> PlaylistScores { get; set; }
    //public DbSet<PlaylistHistory> PlaylistHistories { get; set; }
    public DbSet<RunningTask> RunningTasks { get; set; }
    public DbSet<CompletedTask> CompletedTasks { get; set; }
    public DbSet<ObjectiveCompletionData> ObjectiveCompletionData { get; set; }
    public DbSet<BossModeStats> BossStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SavedDancerCard>(builder =>
        {
            builder.HasKey(card => card.Id);
            builder.HasIndex(card => card.ProfileId);
            builder.HasOne<Profile.Profile>().WithMany().HasForeignKey(card => card.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlayerMapPlay>(builder =>
        {
            builder.HasKey(value => value.Id);
            builder.Property(value => value.MapName).UseCollation("NOCASE").HasMaxLength(128);
            builder.Property(value => value.SourceEventKey).HasMaxLength(160);
            builder.Property(value => value.Platform).HasMaxLength(32);
            builder.Property(value => value.Mode).HasMaxLength(32);
            builder.HasIndex(value => new { value.ProfileId, value.Source, value.SourceEventKey }).IsUnique();
            builder.HasIndex(value => new { value.MapName, value.WeeklyEligible, value.PlayedAtUtc, value.Score });
            builder.HasIndex(value => new { value.ProfileId, value.PlayedAtUtc });
            builder.HasIndex(value => value.PlayedAtUtc);
            builder.HasOne<Profile.Profile>().WithMany().HasForeignKey(value => value.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlayerMapFavorite>(builder =>
        {
            builder.HasKey(value => new { value.ProfileId, value.MapName });
            builder.Property(value => value.MapName).UseCollation("NOCASE").HasMaxLength(128);
            builder.HasOne<Profile.Profile>().WithMany().HasForeignKey(value => value.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlayerMapOrdinal>(builder =>
        {
            builder.HasKey(value => new { value.ProfileId, value.SourceScope, value.MapName });
            builder.Property(value => value.SourceScope).HasMaxLength(128);
            builder.HasIndex(value => value.ObservedAtUtc);
            builder.Property(value => value.MapName).UseCollation("NOCASE").HasMaxLength(128);
            builder.HasOne<Profile.Profile>().WithMany().HasForeignKey(value => value.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Profile.Profile>(builder =>
        {
            builder.HasIndex(profile => profile.Ticket)
                .IsUnique()
                .HasFilter("\"Ticket\" <> ''");
        });

        modelBuilder.Entity<PlatformIdentity>(builder =>
        {
            builder.HasKey(identity => new { identity.Provider, identity.Subject });
            builder.Property(identity => identity.Provider).HasMaxLength(64);
            builder.Property(identity => identity.Subject).HasMaxLength(512);
            builder.Property(identity => identity.PlatformType).HasMaxLength(32);
            builder.Property(identity => identity.IdOnPlatform).HasMaxLength(512);
            builder.Property(identity => identity.NameOnPlatform).HasMaxLength(128);
            builder.HasOne<Profile.Profile>()
                .WithMany()
                .HasForeignKey(identity => identity.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(identity => identity.ProfileId);
            builder.HasIndex(identity => new { identity.PlatformType, identity.IdOnPlatform });
            builder.HasIndex(identity => new { identity.PlatformType, identity.NameOnPlatform });
        });

        modelBuilder.Entity<Friendship>(builder =>
        {
            builder.HasKey(friendship => new { friendship.FirstProfileId, friendship.SecondProfileId });
            builder.HasOne<Profile.Profile>()
                .WithMany()
                .HasForeignKey(friendship => friendship.FirstProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Profile.Profile>()
                .WithMany()
                .HasForeignKey(friendship => friendship.SecondProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(friendship => friendship.SecondProfileId);
            builder.HasIndex(friendship => new { friendship.State, friendship.FirstProfileId });
            builder.HasIndex(friendship => new { friendship.State, friendship.SecondProfileId });
        });

        modelBuilder.Entity<MapStats>(builder =>
        {
            builder.ToTable("MapScores");
            builder.HasKey(score => new { score.MapName, score.ProfileId });
            builder.Property(score => score.MapName)
                .UseCollation("NOCASE")
                .HasMaxLength(128);
            builder.Property(score => score.HighScorePerformance).HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<HighscorePerformance>(value, (JsonSerializerOptions?)null) ?? new());
            builder.Property(score => score.GameModeStats).HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<GameModeStats?>(value, (JsonSerializerOptions?)null));
            builder.HasIndex(score => new { score.MapName, score.HighScore, score.UpdatedAt, score.ProfileId })
                .HasDatabaseName("IX_MapScores_Leaderboard");
            builder.HasIndex(score => score.ProfileId);
        });

        modelBuilder.Entity<ClassicProfile>(builder =>
        {
            builder.HasKey(profile => profile.ProfileId);
            builder.HasOne<Profile.Profile>()
                .WithOne()
                .HasForeignKey<ClassicProfile>(profile => profile.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(profile => profile.CountryId);
        });

        modelBuilder.Entity<PlayerProgression>(builder =>
        {
            builder.HasKey(value => new { value.ProfileId, value.Family });
            builder.HasOne<Profile.Profile>()
                .WithMany()
                .HasForeignKey(value => value.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerUnlock>(builder =>
        {
            builder.HasKey(value => new { value.ProfileId, value.Family, value.UnlockKey });
            builder.Property(value => value.UnlockKey).HasMaxLength(256);
            builder.Property(value => value.Source).HasMaxLength(64);
            builder.HasOne<Profile.Profile>()
                .WithMany()
                .HasForeignKey(value => value.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerQuestProgress>(builder =>
        {
            builder.HasKey(value => new { value.ProfileId, value.Family, value.QuestKey });
            builder.Property(value => value.QuestKey).HasMaxLength(256);
            builder.HasOne<Profile.Profile>()
                .WithMany()
                .HasForeignKey(value => value.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerGachaState>(builder =>
        {
            builder.HasKey(value => new { value.ProfileId, value.Family });
            builder.HasOne<Profile.Profile>()
                .WithMany()
                .HasForeignKey(value => value.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerMetadata>(builder =>
        {
            builder.HasKey(value => value.ProfileId);
            builder.Property(value => value.CountryCode).HasMaxLength(2);
            builder.Property(value => value.Locale).HasMaxLength(16);
            builder.HasOne<Profile.Profile>()
                .WithOne()
                .HasForeignKey<PlayerMetadata>(value => value.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(value => value.CountryId);
            builder.HasIndex(value => value.LastSeenAt);
        });

        modelBuilder.Entity<PlaylistStats>(builder =>
        {
            builder.ToTable("PlaylistScores");
            builder.HasKey(p => new { p.PlaylistId, p.ProfileId });

            ValueComparer<Dictionary<string, int>> dictionaryComparer = new(
                (d1, d2) => d1 != null && d2 != null && d1.Count == d2.Count && !d1.Except(d2).Any(),
                d => d.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key, v.Value)),
                d => new Dictionary<string, int>(d, StringComparer.OrdinalIgnoreCase));

            builder.Property(p => p.HighScorePerMap).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null)
                    ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
                .Metadata.SetValueComparer(dictionaryComparer);
            builder.HasIndex(score => new { score.PlaylistId, score.HighScore, score.UpdatedAt, score.ProfileId })
                .HasDatabaseName("IX_PlaylistScores_Leaderboard");
            builder.HasIndex(score => score.ProfileId);
        });
    }
}
