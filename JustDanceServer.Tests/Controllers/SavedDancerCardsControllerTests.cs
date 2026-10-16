using JustDanceServer.Configuration;
using JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using System.Text.Json;

namespace JustDanceServer.Tests.Controllers;

public sealed class SavedDancerCardsControllerTests : IDisposable
{
    private readonly SessionManager sessions = new(new SecurityService());
    private readonly Mock<ISavedDancerCardService> cards = new(MockBehavior.Strict);
    private readonly Mock<IPlayerMetadataService> metadata = new();
    private readonly Guid owner = Guid.NewGuid();

    [Fact]
    public async Task AllNewRoutesRequireSessionAndDoNotCallStorageOnFailure()
    {
        var controller = Controller(authenticated: false);
        Assert.IsType<UnauthorizedResult>(await controller.GetProfile());
        Assert.IsType<UnauthorizedResult>(await controller.Create(new() { Name = "Guest" }));
        Assert.IsType<UnauthorizedResult>(await controller.Update(Guid.NewGuid(), new()));
        Assert.IsType<UnauthorizedResult>(await controller.Delete(Guid.NewGuid()));
        cards.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ModernProfileMatchesCapturedEnvelopeWithoutChangingLegacyWireShape()
    {
        var primary = Card();
        cards.Setup(service => service.GetAsync(owner)).ReturnsAsync(new SavedDancerCardCollection(primary, [primary]));
        var result = Assert.IsType<OkObjectResult>(await Controller().GetProfile());
        JsonElement json = JsonSerializer.SerializeToElement(result.Value, JsonSerializerOptions.Web);
        JsonElement profile = json.GetProperty("profile");
        Assert.Equal(primary.DancercardId, profile.GetProperty("dancercardId").GetGuid());
        Assert.Equal(primary.Name, profile.GetProperty("name").GetString());
        Assert.Equal(primary.DancercardId, profile.GetProperty("dancercards")[0].GetProperty("dancercardId").GetGuid());
        Assert.True(profile.TryGetProperty("createdAt", out _));
        Assert.True(profile.TryGetProperty("updatedAt", out _));
        Assert.False(profile.TryGetProperty("_id", out _));
        Assert.False(profile.TryGetProperty("dancercard", out _));
        JsonElement legacy = JsonSerializer.SerializeToElement(new DancerCard { Id = owner }, JsonSerializerOptions.Web);
        Assert.Equal(owner, legacy.GetProperty("_id").GetGuid());
        Assert.False(legacy.TryGetProperty("dancercardId", out _));
        Assert.False(legacy.TryGetProperty("dancercards", out _));
        Assert.False(legacy.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task CreateReturns201AndDelete204ForTheSessionOwnerOnly()
    {
        var input = new SavedDancerCardInput { Name = "Guest" };
        var card = Card();
        cards.Setup(service => service.CreateAsync(owner, input)).ReturnsAsync(card);
        cards.Setup(service => service.DeleteAsync(owner, card.DancercardId)).ReturnsAsync(DeleteDancerCardResult.Deleted);
        var controller = Controller();
        var result = Assert.IsType<ObjectResult>(await controller.Create(input));
        Assert.Equal(201, result.StatusCode);
        var json = JsonSerializer.SerializeToElement(result.Value, JsonSerializerOptions.Web);
        Assert.Equal(card.DancercardId, json.GetProperty("dancercard").GetProperty("dancercardId").GetGuid());
        Assert.IsType<NoContentResult>(await controller.Delete(card.DancercardId));
    }

    [Fact]
    public async Task MissingAndPrimaryCardsHaveExplicitNonSuccessResponses()
    {
        var primary = Card();
        cards.Setup(service => service.GetAsync(owner)).ReturnsAsync((SavedDancerCardCollection?)null);
        cards.Setup(service => service.UpdateAsync(owner, primary.DancercardId, It.IsAny<SavedDancerCardInput>()))
            .ReturnsAsync((SavedDancerCardDetails?)null);
        cards.Setup(service => service.DeleteAsync(owner, primary.DancercardId)).ReturnsAsync(DeleteDancerCardResult.PrimaryCard);
        var controller = Controller();
        Assert.IsType<NotFoundResult>(await controller.GetProfile());
        Assert.IsType<NotFoundResult>(await controller.Update(primary.DancercardId, new()));
        Assert.IsType<ConflictObjectResult>(await controller.Delete(primary.DancercardId));
    }

    [Fact]
    public async Task BulkPostPreservesOrderAndMissingEntriesWithoutLeakingPrivateIds()
    {
        Guid missing = Guid.NewGuid();
        var card = Card();
        cards.Setup(service => service.GetPublicAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, SavedDancerCardDetails> { [owner] = card });
        var controller = new SavedDancerCardProfilesController(cards.Object);
        var result = Assert.IsType<OkObjectResult>(await controller.Post(new() { Pids = [missing, owner, missing] }));
        var json = JsonSerializer.SerializeToElement(result.Value, JsonSerializerOptions.Web).GetProperty("profiles");
        Assert.Equal(3, json.GetArrayLength());
        Assert.Equal(missing, json[0].GetProperty("id").GetGuid());
        Assert.Single(json[0].EnumerateObject());
        Assert.Equal(owner, json[1].GetProperty("id").GetGuid());
        Assert.False(json[1].GetProperty("dancercard").TryGetProperty("_id", out _));
        Assert.False(json[1].GetProperty("dancercard").TryGetProperty("dancercardId", out _));
        Assert.False(json[1].GetProperty("dancercard").TryGetProperty("dancercards", out _));
        Assert.Equal(missing, json[2].GetProperty("id").GetGuid());
        Assert.Single(typeof(Profiles).GetMethod(nameof(Profiles.GetProfiles))!
            .GetCustomAttributes(typeof(HttpGetAttribute), false));
    }

    [Fact]
    public async Task InvalidCreateNameDoesNotWrite()
    {
        Assert.IsType<BadRequestObjectResult>(await Controller().Create(new() { Name = " " }));
        cards.VerifyNoOtherCalls();
    }

    private SavedDancerCardsController Controller(bool authenticated = true)
    {
        var controller = new SavedDancerCardsController(cards.Object, sessions, metadata.Object)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext() }
        };
        if (authenticated)
        {
            var (_, id) = sessions.GenerateSession(owner, JdNextConstants.SwitchApplicationId, Guid.NewGuid().ToString());
            controller.Request.Headers["x-jmcs-sessionid"] = id.ToString();
        }

        return controller;
    }

    private static SavedDancerCardDetails Card() => SavedDancerCardDetails.From(new() { Name = "Player" },
        new() { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

    public void Dispose() => sessions.Dispose();
}
