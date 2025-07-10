namespace JustDanceNextPlus.Services;

public class HostedDataService(IServiceProvider serviceProvider) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using IServiceScope scope = serviceProvider.CreateScope();

		// First load the string service
		await CallLoadData<LocalizedStringService>(scope);
		// Then load the tag service, which uses the string service
		await CallLoadData<TagService>(scope);
		// Then load the map service, which uses the tag service and bundle service
		await CallLoadData<MapService>(scope);
		// Then load the playlists, which uses the map service
		await CallLoadData<PlaylistService>(scope);
		// Then load the locker items, which uses the string service
		await CallLoadData<LockerItemsService>(scope);
	}

	static async Task CallLoadData<T>(IServiceScope scope) where T : ILoadService
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
