using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json.Nodes;

namespace JustDanceServer.Tests.Services;

public class HostedDataServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartAsync_RequiresJduAssetsOnlyWhenCatalogIsInstalled(bool catalogInstalled)
    {
        string root = Path.Combine(Path.GetTempPath(), "JustDanceServerTests", Guid.NewGuid().ToString("N"));
        string catalogPath = Path.Combine(root, "catalogs");
        if (catalogInstalled)
            Directory.CreateDirectory(catalogPath);

        try
        {
            Mock<IMapService> mapService = new();
            mapService.SetupGet(service => service.MapToGuid).Returns([]);
            ServiceCollection services = new();
            services.AddLogging();
            services.AddSingleton(Options.Create(new PathSettings
            {
                JduCatalogPath = catalogPath,
                JduCatalogAssetsPath = Path.Combine(root, "assets")
            }));
            services.AddSingleton(Options.Create(new UrlSettings { CDNUrl = "cdn.example.test" }));
            services.AddSingleton(Mock.Of<ILocalizedStringService>());
            services.AddSingleton(Mock.Of<ITagService>());
            services.AddSingleton(mapService.Object);
            services.AddSingleton(Mock.Of<ILockerItemsService>());
            services.AddSingleton(Mock.Of<IEditorialService>());
            services.AddSingleton(Mock.Of<IWdfProgramService>());
            services.AddSingleton(Mock.Of<IPlaylistService>());
            services.AddSingleton(Mock.Of<IActivityPageService>());
            services.AddSingleton(Mock.Of<IMenuDefinitionService>());
            services.AddSingleton<IJduAvatarCatalog, JduAvatarCatalog>();
            services.AddSingleton<JduAvatarWireProjection>();
            services.AddSingleton<IJduCustomizableItemCatalog, JduCustomizableItemCatalog>();
            services.AddSingleton<IJduAliasCatalog, JduAliasCatalog>();
            services.AddSingleton<IJduQuestCatalog, JduQuestCatalog>();
            services.AddSingleton<JduCatalogWireProjection>();
            services.AddSingleton<IJduCatalogService, JduCatalogService>();
            using ServiceProvider provider = services.BuildServiceProvider();
            HostedDataService hostedService = new(provider, Mock.Of<IDatabaseInitializer>());

            if (catalogInstalled)
            {
                await Assert.ThrowsAsync<DirectoryNotFoundException>(() => hostedService.StartAsync(CancellationToken.None));
                return;
            }

            await hostedService.StartAsync(CancellationToken.None);

            IJduCatalogService catalogService = provider.GetRequiredService<IJduCatalogService>();
            foreach (string family in new[]
            {
                JduCatalogFamilies.Items, JduCatalogFamilies.Aliases, JduCatalogFamilies.Quests,
                JduCatalogFamilies.SessionQuests, JduCatalogFamilies.CarouselAvatars,
                JduCatalogFamilies.CarouselDancerProfile, JduCatalogFamilies.CarouselQuests
            })
            {
                Assert.False(catalogService.TryCreateCatalog(family, "jd2022-nx-all", out JsonObject catalog));
                Assert.Empty(catalog);
            }

            Assert.False(catalogService.TryCreateLegacyAvatarCatalog("jd2017-nx-all", out JsonObject avatars));
            Assert.Empty(avatars);
            Assert.False(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task StartAsync_CallsLoadDataOnServicesInCorrectOrder()
    {
        // Arrange
        Mock<ILocalizedStringService> mockLocService = new();
        Mock<ITagService> mockTagService = new();
        Mock<IMapService> mockMapService = new();
        Mock<IDatabaseInitializer> mockDatabaseInitializer = new();
        Mock<ILockerItemsService> mockLockerService = new();
        Mock<IJduCatalogService> mockJduCatalogService = new();
        Mock<IEditorialService> mockEditorialService = new();
        Mock<IWdfProgramService> mockWdfProgramService = new();
        Mock<IPlaylistService> mockPlaylistService = new();
        Mock<IActivityPageService> mockActivityService = new();
        Mock<IMenuDefinitionService> mockMenuService = new();

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
        serviceCollection.AddSingleton(mockJduCatalogService.Object);
        serviceCollection.AddSingleton(mockEditorialService.Object);
        serviceCollection.AddSingleton(mockWdfProgramService.Object);
        serviceCollection.AddSingleton(mockPlaylistService.Object);
        serviceCollection.AddSingleton(mockActivityService.Object);
        serviceCollection.AddSingleton(mockMenuService.Object);

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
        mockJduCatalogService.Verify(s => s.LoadData(), Times.Once);
        mockEditorialService.Verify(s => s.LoadData(), Times.Once);
        mockWdfProgramService.Verify(s => s.LoadData(), Times.Once);
        mockPlaylistService.Verify(s => s.LoadData(), Times.Once);
        mockActivityService.Verify(s => s.LoadData(), Times.Once);
        mockMenuService.Verify(s => s.LoadData(), Times.Once);
    }
}
