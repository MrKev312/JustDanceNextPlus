using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class BundleService
{
	public ShopConfig ShopConfig { get; } = new();

	public BundleService(ILogger<TagService> logger,
		IOptions<PathSettings> pathSettings)
	{
		// Load the bundles
		string bundlesPath = Path.Combine(pathSettings.Value.JsonsPath, "shop-config.json");
		if (!File.Exists(bundlesPath))
		{
			logger.LogInformation("Bundle database not found, creating a new one");
			return;
		}

		string json = File.ReadAllText(bundlesPath);
		ShopConfig? db = JsonSerializer.Deserialize<ShopConfig>(json, JsonSettings.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Bundle database could not be loaded");
			return;
		}

		ShopConfig = db;

		logger.LogInformation("Bundle database loaded");
	}
}
