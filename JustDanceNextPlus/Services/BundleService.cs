using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class BundleService
{
	readonly ILogger<BundleService> logger;
	readonly IOptions<PathSettings> pathSettings;
	readonly JsonSettingsService jsonSettingsService;
	readonly LocalizedStringService localizedStringService;

	public ShopConfig ShopConfig { get; private set; } = new();

	public static List<string> Claims { get; set; } = [];
	public static List<string> ClaimDisplayPriority { get; set; } = [];

	public BundleService(ILogger<BundleService> logger,
		IOptions<PathSettings> pathSettings,
		JsonSettingsService jsonSettingsService,
		LocalizedStringService localizedStringService)
	{
		this.logger = logger;
		this.pathSettings = pathSettings;
		this.jsonSettingsService = jsonSettingsService;
		this.localizedStringService = localizedStringService;

		LoadShopConfig();
	}

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
		string bundlesPath = Path.Combine(pathSettings.Value.JsonsPath, "JustDanceEditions.json");
		if (!File.Exists(bundlesPath))
		{
			logger.LogInformation("Bundle database not found, creating a new one");
			return;
		}

		string json = File.ReadAllText(bundlesPath);
		List<JustDanceEdition>? db = JsonSerializer.Deserialize<List<JustDanceEdition>>(json, jsonSettingsService.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Bundle database could not be loaded");
			return;
		}

		ShopConfig = ParseDatabase(db);

		logger.LogInformation("Bundle database loaded");
	}

	private ShopConfig ParseDatabase(List<JustDanceEdition> db)
	{
		ShopConfig shopConfig = new();
		FirstPartyProductDb database = shopConfig.FirstPartyProductDb;

		List<string> claims = [];

		foreach (JustDanceEdition edition in db)
		{
			// First add it to the dlcProducts
			Guid guid;
			do 
				guid = Guid.NewGuid();
			while (database.DlcProducts.ContainsKey(guid));

			claims.Add(edition.Name);

			DlcProduct product = new()
			{
				ClaimIds = edition.ClaimIds ?? [edition.Name],
				FirstPartyId = edition.Name,
				Name = edition.Name,
				ProductLocId = edition.ProductLocId ?? localizedStringService.GetAddLocalizedTag("Ultimate Edition").OasisIdInt,
				Type = "dlc",
				DlcType = edition.DlcType.ToString().ToLowerInvariant(),
				ProductDescriptionId = edition.ProductDescriptionId ?? localizedStringService.GetAddLocalizedTag("Infinite Just Dance+ access").OasisIdInt
			};

			database.DlcProducts.Add(guid, product);
			string trackExtLocId;
			string trackLimLocId;
			if (edition.DlcType == VersionType.Yearly)
			{
				trackExtLocId = "[VAR:SHOP_TRACKLIST]\n\n[TAG:STRONG]and more![/STRONG]";
				trackLimLocId = "[VAR:SHOP_TRACKLIST]\n[TAG:STRONG]and more![/STRONG]";
			}
			else
			{
				trackExtLocId = "[VAR:SHOP_TRACKLIST]\n[TAG:STRONG]and more![/STRONG]";
				trackLimLocId = "[VAR:SHOP_TRACKLIST]\n[TAG:STRONG]and more![/STRONG]";
			}

			// Then add it to the productGroups
			ProductGroup productGroup = new()
			{
				Type = edition.DlcType.ToString().ToLowerInvariant(),
				DisplayPriority = 0, // Will be updated later
				GroupLocId = edition.GroupLocId,
				Name = edition.Name,
				ProductIds = [guid],
				SongsCountLocId = edition.SongsCountLocId ?? localizedStringService.GetAddLocalizedTag("Unlimited songs").OasisIdInt,
				GroupDescriptionLocId = edition.GroupDescriptionLocId ?? localizedStringService.GetAddLocalizedTag("Get all the songs").OasisIdInt,
				TracklistExtended = edition.TracklistExtended,
				TracklistLimited = edition.TracklistLimited,
				TracklistExtendedLocId = edition.TracklistExtendedLocId ?? localizedStringService.GetAddLocalizedTag(trackExtLocId).OasisIdInt,
				TracklistLimitedLocId = edition.TracklistLimitedLocId ?? localizedStringService.GetAddLocalizedTag(trackLimLocId).OasisIdInt,
				Assets = new()
				{
					ProductGroupBundle = edition.ProductGroupBundle
				}
			};

			database.ProductGroups.Add(guid, productGroup);
		}

		// Get a new guid for the subscription product
		Guid subscriptionGuid;
		do
			subscriptionGuid = Guid.NewGuid();
		while (database.ProductGroups.ContainsKey(subscriptionGuid));

		ProductGroup jdPlus = new()
		{
			Type = "jdplus",
			DisplayPriority = 0, // Will be updated later
			GroupLocId = 0,
			Name = "SUBSCRIPTION_JD+",
			ProductIds = [],
			SongsCountLocId = 0,
			GroupDescriptionLocId = localizedStringService.GetAddLocalizedTag("Access 350+ songs when you subscribe to our extended catalogue").OasisIdInt,
			TracklistExtended = [],
			TracklistLimited = [],
			TracklistExtendedLocId = 0,
			TracklistLimitedLocId = 0,
			Assets = new()
			{
				ProductGroupBundle = "https://jd-s3.cdn.ubi.com/public/jdnext/shop/e788d44c-c411-4222-a7a8-c432e2095c50/nx/productGroupBundle/3d043eb625538fbbc750d2f90ab01872.bundle"
			}
		};

		database.ProductGroups.Add(subscriptionGuid, jdPlus);

		// Sort the claims based on GetClaimLists
		(List<string> songpacks, List<string> otherClaims) = GetClaimLists([.. database.DlcProducts.Values.SelectMany(x => x.ClaimIds)]);
		// Merge them
		claims = [.. songpacks, .. otherClaims, "jdplus"];
		Dictionary<string, int> claimIndex = claims.Select((x, i) => (x, i)).ToDictionary(x => x.x, x => x.i);
		foreach (var group in database.ProductGroups)
		{
			group.Value.DisplayPriority = 1 + (group.Value.Type == "jdplus"
				? claimIndex["jdplus"]
				: group.Value.ProductIds.Select(x => database.DlcProducts[x].Name).Min(x => claimIndex[x]));
		}

		return shopConfig;
	}

	private void InitializeClaims()
	{
		Claims = GetAllClaims();

		(List<string> songpacks, List<string> otherClaims) = GetClaimLists(Claims);

		ClaimDisplayPriority = [.. songpacks, "jdplus", .. otherClaims];
	}

	private static (List<string> songpacks, List<string> otherClaims) GetClaimLists(List<string> claims)
	{
		// First add the songpack_years (2023+) then songpack_games (1-2022)
		List<string> songpacks = [];

		songpacks.AddRange(claims.Where(x => x.StartsWith("songpack_year")).OrderByDescending(x => int.Parse(x[13..])));
		songpacks.AddRange(claims.Where(x => x.StartsWith("songpack_game")).OrderByDescending(x => int.Parse(x[13..])));

		// Now add the other claims to the end
		List<string> otherClaims = [.. claims.Except(songpacks)];
		otherClaims.Sort();

		return (songpacks, otherClaims);
	}

	public List<string> GetAllClaims()
	{
		List<string> claims = [.. ShopConfig.FirstPartyProductDb.DlcProducts.Values
			.SelectMany(x => x.ClaimIds)
			.Distinct()];

		claims.Sort();

		return claims;
	}
}
