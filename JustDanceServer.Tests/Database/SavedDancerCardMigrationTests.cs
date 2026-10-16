using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using System.Text.Json;

namespace JustDanceServer.Tests.Database;

public sealed class SavedDancerCardMigrationTests
{
    [Fact]
    public async Task UpgradeAndDowngradePreserveLegacyProfileAndScores()
    {
        await using SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync();
        MapIdentityMigrationFunctions.Register(connection, new Dictionary<Guid, string>());
        await using UserDataContext db = new(new DbContextOptionsBuilder<UserDataContext>().UseSqlite(connection).Options);
        var migrator = db.GetService<IMigrator>();
        const string previous = "20260922103351_SharedDiscoveryActivity";
        await migrator.MigrateAsync(previous);
        var player = new Profile
        {
            Id = Guid.NewGuid(),
            Ticket = "synthetic-legacy",
            Dancercard = new() { Id = Guid.NewGuid(), Name = "Legacy", Country = "NL", BadgesIds = [Guid.NewGuid()] }
        };
        db.Profiles.Add(player);
        db.MapScores.Add(new() { ProfileId = player.Id, MapName = "SyntheticSong", HighScore = 12345, PlayCount = 7 });
        await db.SaveChangesAsync();
        string original = JsonSerializer.Serialize(player.Dancercard);
        await migrator.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        await new SavedDancerCardService(db, TimeProvider.System).CreateAsync(player.Id, new() { Name = "Guest" });
        Assert.Equal(2, await db.SavedDancerCards.CountAsync());
        await migrator.MigrateAsync(previous);
        db.ChangeTracker.Clear();
        Profile restored = await db.Profiles.Include(p => p.Dancercard).SingleAsync();
        Assert.Equal(original, JsonSerializer.Serialize(restored.Dancercard));
        Assert.Equal("synthetic-legacy", restored.Ticket);
        Assert.Equal(12345, (await db.MapScores.SingleAsync()).HighScore);
        Assert.Equal(7, (await db.MapScores.SingleAsync()).PlayCount);
    }
}
