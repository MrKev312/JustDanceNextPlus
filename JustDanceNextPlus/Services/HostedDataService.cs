namespace JustDanceNextPlus.Services;

public class HostedDataService(IServiceProvider serviceProvider) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using IServiceScope scope = serviceProvider.CreateScope();
		LocalizedStringService localizedStringService = scope.ServiceProvider.GetRequiredService<LocalizedStringService>();
		localizedStringService.LoadData();
		TagService tagService = scope.ServiceProvider.GetRequiredService<TagService>();
		tagService.LoadData();
		MapService dataService = scope.ServiceProvider.GetRequiredService<MapService>();
		await dataService.LoadDataAsync();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		// Implement any cleanup logic if necessary
		return Task.CompletedTask;
	}
}
