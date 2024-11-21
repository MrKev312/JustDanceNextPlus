using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class BundleService(ILogger<TagService> logger,
	IOptions<PathSettings> pathSettings,
	JsonSettingsService jsonSettingsService)
{
	public ShopConfig ShopConfig { get; private set; } = new();

	public static List<string> Claims { get; set; } = [];
	public static List<string> ClaimDisplayPriority { get; set; } = [];

	public void LoadShopConfig()
	{
		// First load the shop config
		LoadData();
		// Then initialize the claims
		InitializeClaims();
	}

	private void LoadData()
	{ 
		// Load the bundles
		string bundlesPath = Path.Combine(pathSettings.Value.JsonsPath, "shop-config.json");
		if (!File.Exists(bundlesPath))
		{
			logger.LogInformation("Bundle database not found, creating a new one");
			return;
		}

		string json = File.ReadAllText(bundlesPath);
		ShopConfig? db = JsonSerializer.Deserialize<ShopConfig>(json, jsonSettingsService.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Bundle database could not be loaded");
			return;
		}

		ShopConfig = db;

		logger.LogInformation("Bundle database loaded");
	}

	private void InitializeClaims()
	{
		Claims = GetAllClaims();

		// First only grab the ones starting with "songpack_year"
		List<string> songpacks = Claims
			.Where(x => x.StartsWith("songpack_year"))
			.ToList();
		// Then sort them by the part after "songpack_year_", numbers below 10 first
		int cutoff = 10;
		songpacks.Sort((x, y) =>
		{
			int xYear = int.Parse(x[13..]);
			int yYear = int.Parse(y[13..]);
			if (xYear < cutoff && yYear >= cutoff)
				return -1;
			if (xYear >= cutoff && yYear < cutoff)
				return 1;
			return yYear.CompareTo(xYear);
		});

		// Now add the other claims to the end
		List<string> otherClaims = Claims
			.Where(x => !x.StartsWith("songpack_year"))
			.ToList();
		otherClaims.Sort();

		ClaimDisplayPriority = [.. songpacks, "jdplus", .. otherClaims];
	}

	public List<string> GetAllClaims()
	{
		List<string> claims = ShopConfig.FirstPartyProductDb.DlcProducts.Values
			.SelectMany(x => x.ClaimIds)
			.Distinct()
			.ToList();

		claims.Sort();

		return claims;
	}
}
