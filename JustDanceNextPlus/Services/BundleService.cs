using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

using System.Collections.Immutable;
using System.Text.Json;

namespace JustDanceNextPlus.Services;

public interface IBundleService : ILoadService
{
    ShopConfig ShopConfig { get; }
    IReadOnlyDictionary<Guid, LiveTile> LiveTiles { get; }
    ImmutableArray<string> Claims { get; }
    ImmutableArray<string> ClaimDisplayPriority { get; }
    ShopConfig GetShopConfig(string platform);
    IReadOnlyDictionary<Guid, LiveTile> GetLiveTiles(string platform);
    ImmutableArray<string> GetAllClaims();
}

public class BundleService(ILogger<BundleService> logger,
        IOptions<PathSettings> pathSettings,
        IOptions<UrlSettings> urlSettings,
        JsonSettingsService jsonSettingsService,
        ILocalizedStringService localizedStringService,
        IFileSystem fileSystem) : IBundleService, ILoadService
{
    public ShopConfig ShopConfig { get; private set; } = new();
    public IReadOnlyDictionary<Guid, LiveTile> LiveTiles { get; private set; } = ImmutableDictionary<Guid, LiveTile>.Empty;
    public ImmutableArray<string> Claims { get; private set; } = [];
    public ImmutableArray<string> ClaimDisplayPriority { get; set; } = [];

    public async Task LoadData()
    {
		// Run file-loading operations in parallel
		Task<ShopConfig> loadShopConfigTask = LoadBundleDatabaseAsync();
		Task<IReadOnlyDictionary<Guid, LiveTile>> loadLiveTilesTask = LoadLiveTiles();

        await Task.WhenAll(loadShopConfigTask, loadLiveTilesTask);

        // Get the results from the pure functions
        ShopConfig shopConfig = await loadShopConfigTask;
        LiveTiles = await loadLiveTilesTask;

        // Calculate dependent properties
        Claims = GetAllClaims(shopConfig);
        ClaimDisplayPriority = CalculateClaimDisplayPriority(Claims);

        // Assign the final, immutable config
        ShopConfig = shopConfig;
    }

    public async Task<IReadOnlyDictionary<Guid, LiveTile>> LoadLiveTiles()
    {
        string liveTilesPath = Path.Combine(pathSettings.Value.JsonsPath, "LiveTileConfig.json");
        if (!fileSystem.FileExists(liveTilesPath))
        {
            logger.LogInformation("Live tiles database not found, skipping load");
            return ImmutableDictionary<Guid, LiveTile>.Empty;
        }

        using Stream fileStream = fileSystem.OpenRead(liveTilesPath);
        ImmutableDictionary<Guid, LiveTile>? liveTiles = await JsonSerializer.DeserializeAsync<ImmutableDictionary<Guid, LiveTile>>(fileStream, jsonSettingsService.PrettyPascalFormat);
        if (liveTiles == null)
        {
            logger.LogWarning("Live tiles database could not be loaded");
            return ImmutableDictionary<Guid, LiveTile>.Empty;
        }

		ImmutableDictionary<Guid, LiveTile> updatedLiveTiles = liveTiles.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value with
            {
                Assets = kvp.Value.Assets with
                {
                    BackgroundImage = BuildLiveTileBundleUrl(ClientPlatform.DefaultPlatform, kvp.Value.Assets.BackgroundImage)
                }
            });

        logger.LogInformation("Live tiles database loaded");
        return updatedLiveTiles;
    }

    private async Task<ShopConfig> LoadBundleDatabaseAsync()
    {
        string bundlesPath = Path.Combine(pathSettings.Value.JsonsPath, "JustDanceEditions.json");
        if (!fileSystem.FileExists(bundlesPath))
        {
            logger.LogInformation("Bundle database not found, creating a new one");
            return new ShopConfig();
        }

        using Stream fileStream = fileSystem.OpenRead(bundlesPath);
        ImmutableList<JustDanceEdition>? db = await JsonSerializer.DeserializeAsync<ImmutableList<JustDanceEdition>>(fileStream, jsonSettingsService.PrettyPascalFormat);

        if (db == null)
        {
            logger.LogWarning("Bundle database could not be loaded");
            return new ShopConfig();
        }

        logger.LogInformation("Bundle database loaded");
        // Call the pure function to parse the database and return a new ShopConfig
        return ParseDatabase(db);
    }

    private ShopConfig ParseDatabase(IReadOnlyList<JustDanceEdition> db)
    {
		ImmutableDictionary<Guid, DlcProduct>.Builder dlcProductsBuilder = ImmutableDictionary.CreateBuilder<Guid, DlcProduct>();
		ImmutableDictionary<Guid, ProductGroup>.Builder productGroupsBuilder = ImmutableDictionary.CreateBuilder<Guid, ProductGroup>();
		List<(Guid Id, ProductGroup Group)> tempProductGroups = [];

        // 1. Build the DlcProducts and initial ProductGroups
        foreach (JustDanceEdition edition in db)
        {
            Guid guid = Guid.NewGuid();
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
            dlcProductsBuilder.Add(guid, product);

            // Determine tracklist loc IDs
            (string trackExtLocId, string trackLimLocId) = edition.DlcType == VersionType.Yearly
                ? ("[VAR:SHOP_TRACKLIST]\n\n[TAG:STRONG]and more![/STRONG]", "[VAR:SHOP_TRACKLIST]\n[TAG:STRONG]and more![/STRONG]")
                : ("[VAR:SHOP_TRACKLIST]\n[TAG:STRONG]and more![/STRONG]", "[VAR:SHOP_TRACKLIST]\n[TAG:STRONG]and more![/STRONG]");

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
                Assets = new() { ProductGroupBundle = BuildVersionBundleUrl(ClientPlatform.DefaultPlatform, edition.Name) }
            };
            tempProductGroups.Add((guid, productGroup));
        }

        // 2. Add the static JD+ product group
        Guid subscriptionGuid = Guid.NewGuid();
		ProductGroup jdPlus = new()
		{
            Type = "jdplus",
            DisplayPriority = 0, // Placeholder
            GroupLocId = localizedStringService.GetLocalizedTag(8747)!,
            Name = "SUBSCRIPTION_JD+",
            ProductIds = [],
            SongsCountLocId = localizedStringService.GetLocalizedTag(0)!,
            GroupDescriptionLocId = localizedStringService.GetAddLocalizedTag("Access 350+ songs when you subscribe to our extended catalogue"),
            TracklistExtended = [],
            TracklistLimited = [],
            TracklistExtendedLocId = localizedStringService.GetLocalizedTag(0)!,
            TracklistLimitedLocId = localizedStringService.GetLocalizedTag(0)!,
            Assets = new() { ProductGroupBundle = BuildVersionBundleUrl(ClientPlatform.DefaultPlatform, "jdplus") }
        };
        tempProductGroups.Add((subscriptionGuid, jdPlus));

		// 3. Calculate display priorities
		ImmutableDictionary<Guid, DlcProduct> dlcProducts = dlcProductsBuilder.ToImmutable();
		IEnumerable<string> allClaimIds = dlcProducts.Values.SelectMany(x => x.ClaimIds);
        (ImmutableArray<string> songpacks, ImmutableArray<string> otherClaims) = GetClaimLists(allClaimIds);
		List<string> sortedClaims = [.. songpacks, .. otherClaims, "jdplus"];
		Dictionary<string, int> claimIndex = sortedClaims.Select((claim, i) => (claim, i)).ToDictionary(x => x.claim, x => x.i);

        // 4. Create final ProductGroup instances with correct priorities
        foreach ((Guid id, ProductGroup group) in tempProductGroups)
        {
            int priority = 1 + (group.Type == "jdplus"
                ? claimIndex["jdplus"]
                : group.ProductIds.Select(pid => dlcProducts[pid].Name).Min(name => claimIndex.GetValueOrDefault(name, int.MaxValue)));

            productGroupsBuilder.Add(id, group with { DisplayPriority = priority });
        }

        // 5. Construct and return the final immutable ShopConfig
        return new ShopConfig
        {
            FirstPartyProductDb = new FirstPartyProductDb
            {
                DlcProducts = dlcProducts,
                ProductGroups = productGroupsBuilder.ToImmutable()
            }
        };
    }

    private static ImmutableArray<string> CalculateClaimDisplayPriority(ImmutableArray<string> claims)
    {
        (ImmutableArray<string> songpacks, ImmutableArray<string> otherClaims) = GetClaimLists(claims);
        return [.. songpacks, "jdplus", .. otherClaims];
    }

    private static (ImmutableArray<string> songpacks, ImmutableArray<string> otherClaims) GetClaimLists(IEnumerable<string> claims)
    {
		ImmutableArray<string> songpacks =
		[
			.. claims.Where(x => x.StartsWith("songpack_year")).OrderByDescending(x => int.Parse(x[13..])),
			.. claims.Where(x => x.StartsWith("songpack_game")).OrderByDescending(x => int.Parse(x[13..])),
		];

		ImmutableArray<string> otherClaims = [.. claims.Except(songpacks).OrderBy(c => c)];

        return (songpacks, otherClaims);
    }

    public ImmutableArray<string> GetAllClaims() => GetAllClaims(ShopConfig);

    public ShopConfig GetShopConfig(string platform)
    {
        ImmutableDictionary<Guid, ProductGroup> productGroups = ShopConfig.FirstPartyProductDb.ProductGroups.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value with
            {
                Assets = kvp.Value.Assets with
                {
                    ProductGroupBundle = BuildVersionBundleUrl(platform, ProductGroupBundleName(kvp.Value))
                }
            });

        return ShopConfig with
        {
            FirstPartyProductDb = ShopConfig.FirstPartyProductDb with
            {
                ProductGroups = productGroups
            }
        };
    }

    public IReadOnlyDictionary<Guid, LiveTile> GetLiveTiles(string platform)
    {
        return LiveTiles.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value with
            {
                Assets = kvp.Value.Assets with
                {
                    BackgroundImage = BuildLiveTileBundleUrl(platform, kvp.Value.Assets.BackgroundImage)
                }
            });
    }

    private static ImmutableArray<string> GetAllClaims(ShopConfig shopConfig)
    {
        return [.. shopConfig.FirstPartyProductDb.DlcProducts.Values
            .SelectMany(x => x.ClaimIds)
            .Distinct()
            .OrderBy(c => c)];
    }

    private string BuildVersionBundleUrl(string platform, string bundleName)
        => BuildBundleUrl(platform, bundleName);

    private string BuildLiveTileBundleUrl(string platform, string bundleReference)
        => BuildBundleUrl(platform, NormalizeBundleName(bundleReference, "live-tile"));

    private string BuildBundleUrl(string platform, string bundleName)
        => $"{CdnBaseUrl}/bundles/{ClientPlatform.Normalize(platform)}/{NormalizeBundleName(bundleName, bundleName)}.bundle";

    private string CdnBaseUrl
    {
        get
        {
            string trimmedUrl = urlSettings.Value.CDNUrl.Trim().TrimEnd('/');
            return trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? trimmedUrl
                : $"https://{trimmedUrl}";
        }
    }

    private static string NormalizeBundleName(string? bundleReference, string fallback)
    {
        if (string.IsNullOrWhiteSpace(bundleReference))
            return fallback;

        string normalizedReference = bundleReference.Trim().Replace('\\', '/');
        if (Uri.TryCreate(normalizedReference, UriKind.Absolute, out Uri? uri))
            normalizedReference = uri.LocalPath;

        string name = Path.GetFileNameWithoutExtension(normalizedReference);
        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }

    private static string ProductGroupBundleName(ProductGroup group)
        => group.Name == "SUBSCRIPTION_JD+" ? "jdplus" : group.Name;
}
