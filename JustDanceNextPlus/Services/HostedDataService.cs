namespace JustDanceNextPlus.Services;

public class HostedDataService(IServiceProvider serviceProvider) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using IServiceScope scope = serviceProvider.CreateScope();

		// First load the string service
		LocalizedStringService localizedStringService = scope.ServiceProvider.GetRequiredService<LocalizedStringService>();
		localizedStringService.LoadData();
		// Then load the tag service, which uses the string service
		TagService tagService = scope.ServiceProvider.GetRequiredService<TagService>();
		tagService.LoadData();
		// Then load the map service, which uses the tag service and bundle service
		MapService dataService = scope.ServiceProvider.GetRequiredService<MapService>();
		await dataService.LoadDataAsync();
		// Then load the playlists
		PlaylistService playlistService = scope.ServiceProvider.GetRequiredService<PlaylistService>();
		playlistService.LoadData();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		// Implement any cleanup logic if necessary
		return Task.CompletedTask;
	}
}
