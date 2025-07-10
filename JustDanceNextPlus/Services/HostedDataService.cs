namespace JustDanceNextPlus.Services;

public class HostedDataService(IServiceProvider serviceProvider) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using IServiceScope scope = serviceProvider.CreateScope();

		await Task.WhenAll(
			LoadDependentServices(scope),
			LoadData<LockerItemsService>(scope));
	}

	private static async Task LoadDependentServices(IServiceScope scope)
	{
		// First load the string service
		await LoadData<LocalizedStringService>(scope);
		// Then load the tag service, which uses the string service
		await LoadData<TagService>(scope);
		// Then load the map service, which uses the tag service and bundle service
		await LoadData<MapService>(scope);
		// Then load the playlists, which uses the map service
		await LoadData<PlaylistService>(scope);
	}

	static async Task LoadData<T>(IServiceScope scope) where T : ILoadService
	{
		T service = scope.ServiceProvider.GetRequiredService<T>();
		await service.LoadData();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		// Implement any cleanup logic if necessary
		return Task.CompletedTask;
	}
}
