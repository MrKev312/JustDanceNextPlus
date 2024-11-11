using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class BundleService
{
	public ShopConfig ShopConfig { get; } = new();

	public BundleService(MapService mapService,
		ILogger<TagService> logger,
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

		// Now from the map service, we can get extra bundle information
		foreach (KeyValuePair<string, List<Guid>> valuePair in mapService.ClaimToGuid)
		{
			// First, get the bundle guid
			string bundleID = valuePair.Key;
			ProductGroup? productGroupGuid;

			if (bundleID != "jdplus")
			{
				Guid bundleGuid = db.FirstPartyProductDb.DlcProducts.FirstOrDefault(x => x.Value.ClaimIds.Contains(bundleID)).Key;

				if (bundleGuid == Guid.Empty)
				{
					logger.LogWarning("Bundle not found in the bundle database: {bundleID}", bundleID);
					continue;
				}

				productGroupGuid = db.FirstPartyProductDb.ProductGroups.FirstOrDefault(x => x.Value.ProductIds.Contains(bundleGuid)).Value;
			}
			else
			{
				productGroupGuid = db.FirstPartyProductDb.ProductGroups[Guid.Parse("e788d44c-c411-4222-a7a8-c432e2095c50")];
			}

			if (productGroupGuid == null)
			{
				logger.LogWarning("Product group not found in the bundle database: {bundleID}", bundleID);
				continue;
			}

			// Now, add the songs to the bundle
			for (int i = 0; i < valuePair.Value.Count && productGroupGuid.TracklistLimited.Count < 6; i++)
			{
				Guid songId = valuePair.Value[i];
				if (!productGroupGuid.TracklistLimited.Contains(songId))
					productGroupGuid.TracklistLimited.Add(songId);
			}

			// If the extended tracklist is not full, add the limited tracklist to it
			if (productGroupGuid.TracklistExtended.Count < 10)
			{
				for (int i = 0; i < productGroupGuid.TracklistLimited.Count && productGroupGuid.TracklistExtended.Count < 10; i++)
				{
					Guid songId = productGroupGuid.TracklistLimited[i];
					if (!productGroupGuid.TracklistExtended.Contains(songId))
						productGroupGuid.TracklistExtended.Add(songId);
				}
			}

			// If the extended tracklist is still not full, add the rest of the songs
			for (int i = 0; i < valuePair.Value.Count && productGroupGuid.TracklistExtended.Count < 10; i++)
			{
				Guid songId = valuePair.Value[i];
				if (!productGroupGuid.TracklistExtended.Contains(songId))
					productGroupGuid.TracklistExtended.Add(songId);
			}
		}

		ShopConfig = db;

		logger.LogInformation("Bundle database loaded");
	}
}
