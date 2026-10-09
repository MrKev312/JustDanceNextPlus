using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.GraphQL;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using Scalar.AspNetCore;

namespace JustDanceServer;

public class Program
{
	public static void Main(string[] args)
	{

		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		PemCertificateConfiguration.Configure(builder);

		// Add services to the container.
		ConfigureServices(builder);

		// Check if JSON files exist.
		if (!CheckJsonExistance(builder.Configuration["Paths:JsonsPath"]!))
		{
			Console.WriteLine("JSON files are missing. Exiting...");
			return;
		}

		// Ensure the maps folder exists.
		Directory.CreateDirectory(builder.Configuration["Paths:MapsPath"]!);

		WebApplication app = builder.Build();

		// Configure the HTTP request pipeline.
		ConfigureMiddleware(app);

		app.MapGet("/", () => "Hello, World!");

		app.Run();
	}

	private static void ConfigureServices(WebApplicationBuilder builder)
	{
		// Add controllers
		builder.Services.AddControllers();
		builder.Services.Configure<RouteOptions>(options =>
		{
			options.ConstraintMap["jdnext"] = typeof(JdNextSpaceRouteConstraint);
			options.ConstraintMap["jdu"] = typeof(JduSpaceRouteConstraint);
		});

		// Learn more about configuring OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();

		// Allow gzip compression.
		builder.Services.AddResponseCompression(options =>
		{
			options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
				"application/json",
				"application/json; charset=utf-8",
				"application/graphql",
				"application/graphql-response+json",
				"text/json"
				]);
			options.EnableForHttps = true;
		});

		// Bind configuration.
		builder.Services.Configure<PathSettings>(builder.Configuration.GetSection("Paths"));
		builder.Services.Configure<UrlSettings>(builder.Configuration.GetSection("HostingUrls"));
		builder.Services.Configure<ClassicJduDiagnosticsSettings>(
			builder.Configuration.GetSection("ClassicJduDiagnostics"));
		builder.Services.Configure<PlatformAuthenticationSettings>(
			builder.Configuration.GetSection("PlatformAuthentication"));
		builder.Services.AddHttpClient("NintendoJwks")
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				AllowAutoRedirect = false
			});

		// Configure Entity Framework with SQLite.
		SqliteConnectionStringBuilder connectionString = new()
		{
			DataSource = builder.Configuration["Paths:UserDataPath"],
			Cache = SqliteCacheMode.Shared,
			Pooling = true,
			DefaultTimeout = 30
		};
		builder.Services.AddDbContext<UserDataContext>(options =>
			options.UseSqlite(connectionString.ToString()));

		// Inject custom services.
		builder.Services.AddSingleton<IFileSystem, SystemFileSystem>();
        builder.Services.AddScoped<IUserDataService, UserDataService>();
		builder.Services.AddScoped<IUserProfileService, UserProfileService>();
		builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
		builder.Services.AddScoped<IScoreService, ScoreService>();
		builder.Services.AddScoped<IClassicProfileService, ClassicProfileService>();
		builder.Services.AddScoped<IProgressionService, ProgressionService>();
		builder.Services.AddScoped<IPlayerMetadataService, PlayerMetadataService>();
		builder.Services.AddScoped<IPlayerIdentityService, PlayerIdentityService>();
		builder.Services.AddSingleton<IPlatformTicketVerifier, NintendoPlatformTicketVerifier>();
		builder.Services.AddSingleton<IPlatformTicketVerifier, DevelopmentPlatformTicketVerifier>();
		builder.Services.AddSingleton<IPlatformTicketVerifierRegistry, PlatformTicketVerifierRegistry>();
		builder.Services.AddSingleton<ISessionLoginIdentityParser, SessionLoginIdentityParser>();
		builder.Services.AddSingleton<IBasicCredentialVerifier, BasicCredentialVerifier>();
		builder.Services.AddScoped<IPlayerActivityService, PlayerActivityService>();
		builder.Services.AddScoped<IMapCompletionService, MapCompletionService>();
		builder.Services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
		builder.Services.AddScoped<IJduRecommendationService, JduRecommendationService>();
		builder.Services.AddScoped<IRecommendationService, RecommendationService>();
		builder.Services.AddScoped<IPlayerPreferenceService, PlayerPreferenceService>();
		builder.Services.AddScoped<IMenuService, MenuService>();
		builder.Services.AddSingleton<IMenuDefinitionService, MenuDefinitionService>();
		builder.Services.AddSingleton<IEditorialService, EditorialService>();
		builder.Services.AddSingleton<IJduPackageService, JduPackageService>();
		builder.Services.AddSingleton<IJduContentAuthorizationService, JduContentAuthorizationService>();
		builder.Services.AddSingleton<IJduAvatarCatalog, JduAvatarCatalog>();
		builder.Services.AddSingleton<JduAvatarWireProjection>();
		builder.Services.AddSingleton<IJduCustomizableItemCatalog, JduCustomizableItemCatalog>();
		builder.Services.AddSingleton<IJduAliasCatalog, JduAliasCatalog>();
		builder.Services.AddSingleton<IJduQuestCatalog, JduQuestCatalog>();
		builder.Services.AddSingleton<JduCatalogWireProjection>();
		builder.Services.AddSingleton<IJduCatalogService, JduCatalogService>();
		builder.Services.AddSingleton<IClassicJduConfigurationService, ClassicJduConfigurationService>();
		builder.Services.AddSingleton<IClassicJduDiscoveryService, ClassicJduDiscoveryService>();
		builder.Services.AddSingleton<IClassicJduSongDatabaseService, ClassicJduSongDatabaseService>();
		builder.Services.AddSingleton<IClassicJduConstantService, ClassicJduConstantService>();
		builder.Services.AddSingleton<IClassicJduMenuService, ClassicJduMenuService>();
		builder.Services.AddSingleton<IModernJduSongDatabaseService, ModernJduSongDatabaseService>();
		builder.Services.AddSingleton<IModernJduMenuService, ModernJduMenuService>();
		builder.Services.AddSingleton<IModernJduApplicationConfigurationService, ModernJduApplicationConfigurationService>();
		builder.Services.AddSingleton<IModernJduDiscoveryService, ModernJduDiscoveryService>();
		builder.Services.AddSingleton(TimeProvider.System);
		builder.Services.AddSingleton<ILocalizedStringService, LocalizedStringService>();
		builder.Services.AddSingleton<ITagService, TagService>();
        builder.Services.AddSingleton<IMapService, MapService>();
        builder.Services.AddSingleton<IBundleService, BundleService>();
		builder.Services.AddSingleton<IPlaylistService, PlaylistService>();
        builder.Services.AddSingleton<ILockerItemsService, LockerItemsService>();
        builder.Services.AddSingleton<IActivityPageService, ActivityPageService>();
		builder.Services.AddSingleton<IPartyManager, PartyManager>();
		builder.Services.AddSingleton<ISecurityService, SecurityService>();
		builder.Services.AddSingleton<ISessionManager, SessionManager>();
		builder.Services.AddSingleton<IPairingService, PairingService>();
		builder.Services.AddSingleton<ITimingService, TimingService>();
		builder.Services.AddSingleton<IServerMapLoader, ServerMapLoader>();
		builder.Services.AddSingleton<IMapMediaLoader, MapMediaLoader>();
		builder.Services.AddSingleton<IVideoFileInspector, VideoFileInspector>();
		builder.Services.AddSingleton<IMapPackageReader, MapPackageReader>();
		builder.Services.AddSingleton<IJdPlusAssetMetadataLoader, JdPlusAssetMetadataLoader>();
        builder.Services.AddSingleton<IWebmExtractor, WebmExtractor>();

		// Inject json converters.
        builder.Services.AddSingleton<JsonSettingsService>();
		builder.Services.AddSingleton<TagIdConverter>();
		builder.Services.AddSingleton<GuidTagConverter>();
		builder.Services.AddSingleton<MapTagConverter>();
		builder.Services.AddSingleton<MapTagListConverter>();

		builder.Services
			.AddGraphQLServer()
			.AddQueryType<Query>()
			.AddMutationType<Mutation>();

		// Add hosted service.
		builder.Services.AddHostedService<HostedDataService>();

		// Add HTTP context accessor.
		builder.Services.AddHttpContextAccessor();

		// Add response caching if not in development environment.
		if (!builder.Environment.IsDevelopment())
		{
			builder.Services.AddResponseCaching();
		}

		// Add MVC with custom JSON options.
		builder.Services.AddSingleton<IConfigureOptions<JsonOptions>, ConfigureJsonOptions>();
		builder.Services.AddMvc();
	}

	private static void ConfigureMiddleware(WebApplication app)
	{
#if !RELEASE
		app.MapOpenApi();
        app.MapScalarApiReference();
#endif

		app.UseHttpsRedirection();
		app.UseResponseCompression();

		// Configure static file serving with custom content types.
		FileExtensionContentTypeProvider provider = new();
		provider.Mappings[".bundle"] = "application/octet-stream";
		provider.Mappings[".webm"] = "video/webm";
		provider.Mappings[".opus"] = "audio/opus";
		provider.Mappings[".ogg"] = "audio/ogg";
		provider.Mappings[".zip"] = "application/zip";
		provider.Mappings[".ckd"] = "application/octet-stream";

        // Serve static files.
        app.UseStaticFiles(new StaticFileOptions
		{
			ContentTypeProvider = provider
		});

        // Add external maps middleware
		string? jduCatalogAssetsPath = app.Configuration["Paths:JduCatalogAssetsPath"];
		if (!string.IsNullOrWhiteSpace(jduCatalogAssetsPath)
			&& Directory.Exists(jduCatalogAssetsPath))
		{
			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(Path.GetFullPath(jduCatalogAssetsPath)),
				RequestPath = $"/{CdnContentPaths.JduCatalogAssetsRoot}",
				ContentTypeProvider = provider
			});
		}

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.GetFullPath(app.Configuration["Paths:MapsPath"]!)),
            RequestPath = "/maps",
            ContentTypeProvider = provider
        });

        // Dynamic responses may depend on session and platform headers.
        if (!app.Environment.IsDevelopment())
		{
			app.UseMiddleware<ApiCachePolicyMiddleware>();
		}

		app.MapControllers();
		app.MapGraphQL("/profile/v3/graphql");
	}

	public static bool CheckJsonExistance(string jsonPath)
	{
		string[] paths = [
			"activity-page.json",
			"JustDanceEditions.json"
			];

		bool missing = false;

		foreach (string path in paths)
		{
			if (!File.Exists(Path.Combine(jsonPath, path))) 
			{
				Console.WriteLine($"Missing JSON file: {path}");
				missing = true;
			}
		}

		return !missing;
	}
}
