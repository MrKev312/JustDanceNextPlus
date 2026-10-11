using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JustDanceServer.Tests.Database;

public sealed class SharedDiscoveryMigrationTests
{
    [Fact]
    public async Task MigrationPreservesExistingPlayersScoresAndProgressionAndEnforcesActivityUniqueness()
    {
        await using SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync();
        MapIdentityMigrationFunctions.Register(connection, new Dictionary<Guid, string>());
        await using UserDataContext db = new(new DbContextOptionsBuilder<UserDataContext>().UseSqlite(connection).Options);
        IMigrator migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260904210000_UnifyServerDataModel");
        Guid player = Guid.NewGuid();
        db.Profiles.Add(new Profile { Id = player, Dancercard = new() { Id = player, Name = "Existing" } });
        db.MapScores.Add(new() { ProfileId = player, MapName = "Alpha", HighScore = 12345, PlayCount = 7 });
        db.PlayerProgressions.Add(new() { ProfileId = player, Family = ProgressionFamily.JDPlus, Xp = 4321, Level = 8 });
        await db.SaveChangesAsync();
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        Assert.Equal("Existing", (await db.Profiles.Include(value => value.Dancercard).SingleAsync()).Dancercard.Name);
        Assert.Equal(7, (await db.MapScores.SingleAsync()).PlayCount);
        Assert.Equal(4321, (await db.PlayerProgressions.SingleAsync()).Xp);
        Assert.Empty(await db.PlayerMapPlays.ToListAsync());
        db.PlayerMapFavorites.Add(new() { ProfileId = player, MapName = "Alpha", FavoritedAtUtc = DateTime.UtcNow });
        db.PlayerMapOrdinals.Add(new() { ProfileId = player, SourceScope = "legacy", MapName = "Alpha", Ordinal = 12, ObservedAtUtc = DateTime.UtcNow });
        db.PlayerMapPlays.Add(new() { Id = Guid.NewGuid(), ProfileId = player, MapName = "Alpha", SourceEventKey = "event" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        db.PlayerMapPlays.Add(new() { Id = Guid.NewGuid(), ProfileId = player, MapName = "Alpha", SourceEventKey = "event" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        await db.Profiles.Where(value => value.Id == player).ExecuteDeleteAsync();
        Assert.Empty(await db.PlayerMapPlays.ToListAsync());
        Assert.Empty(await db.PlayerMapFavorites.ToListAsync());
        Assert.Empty(await db.PlayerMapOrdinals.ToListAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
