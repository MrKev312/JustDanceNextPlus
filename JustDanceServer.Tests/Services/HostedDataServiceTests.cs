using JustDanceServer.Services;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace JustDanceServer.Tests.Services;

public class HostedDataServiceTests
{
    [Fact]
    public async Task StartAsync_CallsLoadDataOnServicesInCorrectOrder()
    {
		// Arrange
		Mock<ILocalizedStringService> mockLocService = new();
		Mock<ITagService> mockTagService = new();
		Mock<IMapService> mockMapService = new();
		Mock<IDatabaseInitializer> mockDatabaseInitializer = new();
		Mock<ILockerItemsService> mockLockerService = new();
		Mock<IPlaylistService> mockPlaylistService = new();
		Mock<IActivityPageService> mockActivityService = new();

		// Use a sequence to verify call order
		MockSequence sequence = new();
        mockLocService.InSequence(sequence).Setup(s => s.LoadData()).Returns(Task.CompletedTask);
        mockTagService.InSequence(sequence).Setup(s => s.LoadData()).Returns(Task.CompletedTask);
        mockMapService.InSequence(sequence).Setup(s => s.LoadData()).Returns(Task.CompletedTask);
		mockDatabaseInitializer.InSequence(sequence)
			.Setup(service => service.InitializeAsync(CancellationToken.None))
			.Returns(Task.CompletedTask);

		ServiceCollection serviceCollection = new();
        serviceCollection.AddSingleton(mockLocService.Object);
        serviceCollection.AddSingleton(mockTagService.Object);
        serviceCollection.AddSingleton(mockMapService.Object);
        serviceCollection.AddSingleton(mockLockerService.Object);
        serviceCollection.AddSingleton(mockPlaylistService.Object);
        serviceCollection.AddSingleton(mockActivityService.Object);

		ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
		HostedDataService hostedService = new(serviceProvider, mockDatabaseInitializer.Object);

        // Act
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        // The sequence mock handles the order verification for serialization-dependent services.
        mockLocService.Verify(s => s.LoadData(), Times.Once);
        mockTagService.Verify(s => s.LoadData(), Times.Once);
        mockMapService.Verify(s => s.LoadData(), Times.Once);
		mockDatabaseInitializer.Verify(
			service => service.InitializeAsync(CancellationToken.None),
			Times.Once);

        // Verify the other services were also called.
        mockLockerService.Verify(s => s.LoadData(), Times.Once);
        mockPlaylistService.Verify(s => s.LoadData(), Times.Once);
        mockActivityService.Verify(s => s.LoadData(), Times.Once);
    }
}
