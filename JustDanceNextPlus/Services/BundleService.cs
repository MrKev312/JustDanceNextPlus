using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class BundleService : ILoadService
{
	private readonly ILogger<BundleService> logger;
	private readonly IOptions<PathSettings> pathSettings;
	private readonly IOptions<UrlSettings> urlSettings;
	private readonly JsonSettingsService jsonSettingsService;
	private readonly LocalizedStringService localizedStringService;

	public BundleService(ILogger<BundleService> logger,
		IOptions<PathSettings> pathSettings,
		IOptions<UrlSettings> urlSettings,
		JsonSettingsService jsonSettingsService,
		LocalizedStringService localizedStringService)
	{
		this.logger = logger;
		this.pathSettings = pathSettings;
		this.urlSettings = urlSettings;
		this.jsonSettingsService = jsonSettingsService;
		this.localizedStringService = localizedStringService;

		LoadData().GetAwaiter().GetResult();
	}

	public ShopConfig ShopConfig { get; private set; } = new();
	public Dictionary<Guid, LiveTile> LiveTiles { get; private set; } = [];

	public static List<string> Claims { get; set; } = [];
	public static List<string> ClaimDisplayPriority { get; set; } = [];

	public async Task LoadData()
	{
		await Task.WhenAll(
			LoadJustDanceBundles(),
			LoadLiveTiles()
		);
	}

	public async Task LoadLiveTiles()
	{
		string liveTilesPath = Path.Combine(pathSettings.Value.JsonsPath, "LiveTileConfig.json");
		if (!File.Exists(liveTilesPath))
		{
			logger.LogInformation("Live tiles database not found, creating a new one");
			return;
		}

		using FileStream fileStream = File.OpenRead(liveTilesPath);
		Dictionary<Guid, LiveTile>? liveTiles = await JsonSerializer.DeserializeAsync<Dictionary<Guid, LiveTile>>(fileStream, jsonSettingsService.PrettyPascalFormat);
		if (liveTiles == null)
		{
			logger.LogWarning("Live tiles database could not be loaded");
			return;
		}

		// Replace the CDN URL in the assets
		foreach (KeyValuePair<Guid, LiveTile> tile in liveTiles)
		{
			tile.Value.Assets.BackgroundImage = tile.Value.Assets.BackgroundImage.Replace("{{cdnUrl}}", urlSettings.Value.CDNUrl);
		}

		LiveTiles = liveTiles;
		logger.LogInformation("Live tiles database loaded");
	}

	private async Task LoadJustDanceBundles()
	{
		// First load the shop config
		await LoadBundleDatabase();
		// Then initialize the claims
		InitializeClaims();
	}

	private async Task LoadBundleDatabase()
	{ 
		// Load the bundles
		string bundlesPath = Path.Combine(pathSettings.Value.JsonsPath, "JustDanceEditions.json");
		if (!File.Exists(bundlesPath))
		{
			logger.LogInformation("Bundle database not found, creating a new one");
			return;
		}

		using FileStream fileStream = File.OpenRead(bundlesPath);
		List<JustDanceEdition>? db = await JsonSerializer.DeserializeAsync<List<JustDanceEdition>>(fileStream, jsonSettingsService.PrettyPascalFormat);

		if (db == null)
		{
			logger.LogWarning("Bundle database could not be loaded");
			return;
		}

		// Now we replace the CDN URL in the assets
		db.ForEach(edition => edition.ProductGroupBundle = edition.ProductGroupBundle.Replace("{{cdnUrl}}", urlSettings.Value.CDNUrl));

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
				ProductLocId = edition.ProductLocId ?? localizedStringService.GetAddLocalizedTag("Ultimate Edition"),
				Type = "dlc",
				DlcType = edition.DlcType.ToString().ToLowerInvariant(),
				ProductDescriptionId = edition.ProductDescriptionId ?? localizedStringService.GetAddLocalizedTag("Infinite Just Dance+ access")
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
				SongsCountLocId = edition.SongsCountLocId ?? localizedStringService.GetAddLocalizedTag("Unlimited songs"),
				GroupDescriptionLocId = edition.GroupDescriptionLocId ?? localizedStringService.GetAddLocalizedTag("Get all the songs"),
				TracklistExtended = edition.TracklistExtended,
				TracklistLimited = edition.TracklistLimited,
				TracklistExtendedLocId = edition.TracklistExtendedLocId ?? localizedStringService.GetAddLocalizedTag(trackExtLocId),
				TracklistLimitedLocId = edition.TracklistLimitedLocId ?? localizedStringService.GetAddLocalizedTag(trackLimLocId),
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
			GroupLocId = localizedStringService.GetLocalizedTag(8747)!,
			Name = "SUBSCRIPTION_JD+",
			ProductIds = [],
			SongsCountLocId = localizedStringService.GetLocalizedTag(0)!,
			GroupDescriptionLocId = localizedStringService.GetAddLocalizedTag("Access 350+ songs when you subscribe to our extended catalogue"),
			TracklistExtended = [],
			TracklistLimited = [],
			TracklistExtendedLocId = localizedStringService.GetLocalizedTag(0)!,
			TracklistLimitedLocId = localizedStringService.GetLocalizedTag(0)!,
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
		foreach (KeyValuePair<Guid, ProductGroup> group in database.ProductGroups)
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
