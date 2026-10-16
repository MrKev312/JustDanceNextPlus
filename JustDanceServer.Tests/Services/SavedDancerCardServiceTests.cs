using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using System.Text.Json;

namespace JustDanceServer.Tests.Services;

public sealed class SavedDancerCardServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
    private UserDataContext db = null!;
    private SavedDancerCardService service = null!;
    private readonly Profile player = NewProfile("Legacy player");
    private readonly Profile other = NewProfile("Other player");

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        db = NewContext();
        await db.Database.EnsureCreatedAsync();
        db.Profiles.AddRange(player, other);
        await db.SaveChangesAsync();
        service = new(db, TimeProvider.System);
    }

    [Fact]
    public async Task ExistingProfileGetsOneStablePrimaryWithoutChangingLegacyCard()
    {
        string before = JsonSerializer.Serialize(player.Dancercard);
        var first = await service.GetAsync(player.Id);
        var second = await service.GetAsync(player.Id);
        Assert.NotNull(first);
        Assert.Equal(first.Primary, Assert.Single(first.Cards));
        Assert.Equal(player.Dancercard.Id, first.Primary.DancercardId);
        Assert.Equal(first.Primary.CreatedAt, second!.Primary.CreatedAt);
        Assert.Single(await db.SavedDancerCards.ToListAsync());
        Assert.Equal(before, JsonSerializer.Serialize(player.Dancercard));
    }

    [Fact]
    public async Task ExtraCardsPersistAcrossContextsAndNeverReplaceThePrimary()
    {
        var added = await service.CreateAsync(player.Id, new() { Name = "Guest", Country = "UBI" });
        Assert.NotNull(added);
        Assert.NotEqual(player.Dancercard.Id, added.DancercardId);
        await using var fresh = NewContext();
        var result = await new SavedDancerCardService(fresh, TimeProvider.System).GetAsync(player.Id);
        Assert.Equal(2, result!.Cards.Count);
        Assert.Contains(result.Cards, card => card.Name == "Guest" && card.Country == "UBI");
        Assert.Equal("Legacy player", result.Primary.Name);
        Assert.All(result.Cards, card =>
        {
            Assert.Equal(DateTimeKind.Utc, card.CreatedAt.Kind);
            Assert.Equal(DateTimeKind.Utc, card.UpdatedAt.Kind);
            var json = JsonSerializer.SerializeToElement(card, JsonSerializerOptions.Web);
            Assert.EndsWith("Z", json.GetProperty("createdAt").GetString());
            Assert.EndsWith("Z", json.GetProperty("updatedAt").GetString());
        });
    }

    [Fact]
    public async Task PartialUpdatePreservesOmittedFieldsAndServerAssignedIdentity()
    {
        Guid avatar = Guid.NewGuid();
        Guid badge = Guid.NewGuid();
        var added = await service.CreateAsync(player.Id, new()
        {
            Name = "Guest",
            AvatarId = avatar,
            Country = "NL",
            BadgesIds = [badge]
        });
        var updated = await service.UpdateAsync(player.Id, added!.DancercardId, new() { Name = "Renamed" });
        Assert.Equal(added.DancercardId, updated!.DancercardId);
        Assert.Equal(added.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= updated.CreatedAt);
        Assert.Equal(avatar, updated.AvatarId);
        Assert.Equal("NL", updated.Country);
        Assert.Equal(badge, Assert.Single(updated.BadgesIds));
        Assert.Equal("Legacy player", player.Dancercard.Name);
    }

    [Fact]
    public async Task PrimaryEditsAreVisibleToOldClientsAndOldEditsAreVisibleToNewClients()
    {
        var primary = (await service.GetAsync(player.Id))!.Primary;
        await service.UpdateAsync(player.Id, primary.DancercardId, new() { Name = "Modern edit", Country = "FR" });
        await using var oldContext = NewContext();
        UserProfileService legacy = new(oldContext, NullLogger<UserProfileService>.Instance);
        Profile oldView = (await legacy.GetProfileByIdAsync(player.Id))!;
        Assert.Equal("Modern edit", oldView.Dancercard.Name);
        Assert.Equal("FR", oldView.Dancercard.Country);
        oldView.Dancercard.Name = "Legacy edit";
        oldView.Dancercard.Id = Guid.NewGuid(); // A client cannot replace its database identity.
        Assert.True(await legacy.UpdateProfileAsync(oldView));
        await using var newContext = NewContext();
        var newView = await new SavedDancerCardService(newContext, TimeProvider.System).GetAsync(player.Id);
        Assert.Equal("Legacy edit", newView!.Primary.Name);
        Assert.Equal(primary.DancercardId, newView.Primary.DancercardId);
        Assert.Equal(primary.CreatedAt, newView.Primary.CreatedAt);
    }

    [Fact]
    public async Task OtherPlayersCardsCannotBeReadEditedOrDeleted()
    {
        var added = await service.CreateAsync(player.Id, new() { Name = "Private guest" });
        Assert.Null(await service.UpdateAsync(other.Id, added!.DancercardId, new() { Name = "Attack" }));
        Assert.Equal(DeleteDancerCardResult.NotFound, await service.DeleteAsync(other.Id, added.DancercardId));
        var collection = await service.GetAsync(other.Id);
        Assert.Single(collection!.Cards);
        Assert.DoesNotContain(collection.Cards, card => card.DancercardId == added.DancercardId);
        Assert.Null(await service.UpdateAsync(other.Id, player.Dancercard.Id, new() { Name = "Attack" }));
        Assert.Equal("Legacy player", player.Dancercard.Name);
    }

    [Fact]
    public async Task DeletingGuestKeepsPrimaryAndPrimaryCannotBeDeleted()
    {
        var added = await service.CreateAsync(player.Id, new() { Name = "Guest" });
        Assert.Equal(DeleteDancerCardResult.Deleted, await service.DeleteAsync(player.Id, added!.DancercardId));
        Assert.Equal(DeleteDancerCardResult.NotFound, await service.DeleteAsync(player.Id, added.DancercardId));
        Assert.Equal(DeleteDancerCardResult.PrimaryCard, await service.DeleteAsync(player.Id, player.Dancercard.Id));
        Assert.Single((await service.GetAsync(player.Id))!.Cards);
        Assert.NotNull(await db.Profiles.FindAsync(player.Id));
    }

    [Fact]
    public async Task MissingProfileDoesNotCreateAnyState()
    {
        Guid absent = Guid.NewGuid();
        Assert.Null(await service.GetAsync(absent));
        Assert.Null(await service.CreateAsync(absent, new() { Name = "Absent" }));
        Assert.Null(await service.UpdateAsync(absent, Guid.NewGuid(), new() { Name = "Absent" }));
        Assert.Equal(DeleteDancerCardResult.NotFound, await service.DeleteAsync(absent, Guid.NewGuid()));
        Assert.Empty(await db.SavedDancerCards.ToListAsync());
    }

    [Fact]
    public async Task PublicLookupReturnsOnlyPrimaryCardsWithoutCreatingState()
    {
        var found = await service.GetPublicAsync([player.Id, Guid.NewGuid(), player.Id]);
        Assert.Equal("Legacy player", Assert.Single(found).Value.Name);
        Assert.Empty(await db.SavedDancerCards.ToListAsync());
        await service.CreateAsync(player.Id, new() { Name = "Private guest" });
        found = await service.GetPublicAsync([player.Id]);
        Assert.Equal("Legacy player", Assert.Single(found).Value.Name);
    }

    [Fact]
    public async Task DeletingProfileCascadesOnlyItsSavedCards()
    {
        await service.CreateAsync(player.Id, new() { Name = "Guest" });
        await service.GetAsync(other.Id);
        db.Profiles.Remove(player);
        await db.SaveChangesAsync();
        Assert.Equal(other.Id, (await db.SavedDancerCards.SingleAsync()).ProfileId);
    }

    private UserDataContext NewContext() => new(new DbContextOptionsBuilder<UserDataContext>().UseSqlite(connection).Options);

    private static Profile NewProfile(string name) => new()
    {
        Id = Guid.NewGuid(),
        Ticket = Guid.NewGuid().ToString(),
        Dancercard = new() { Id = Guid.NewGuid(), Name = name, Country = "NL" }
    };

    public async Task DisposeAsync()
    {
        await db.DisposeAsync();
        await connection.DisposeAsync();
    }
}
