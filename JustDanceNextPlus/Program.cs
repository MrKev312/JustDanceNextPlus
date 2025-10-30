using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using Scalar.AspNetCore;

namespace JustDanceNextPlus;

public class Program
{
	public static void Main(string[] args)
	{

		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

		// Ensure the database is created.
		InitializeDatabase(app);

		// Configure the HTTP request pipeline.
		ConfigureMiddleware(app);

		app.MapGet("/", () => "Hello, World!");

		app.Run();
	}

	private static void ConfigureServices(WebApplicationBuilder builder)
	{
		// Add controllers
		builder.Services.AddControllers();

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

		// Configure Entity Framework with SQLite.
		builder.Services.AddDbContext<UserDataContext>(options =>
			options.UseLazyLoadingProxies()
				   .UseSqlite($"Data Source={builder.Configuration["Paths:UserDataPath"]}"));

		// Inject custom services.
		builder.Services.AddSingleton<IFileSystem, SystemFileSystem>();
        builder.Services.AddScoped<IUserDataService, UserDataService>();
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
		builder.Services.AddSingleton<ITimingService, TimingService>();
		builder.Services.AddSingleton<IUtilityService, UtilityService>();
        builder.Services.AddSingleton<IWebmExtractor, WebmExtractor>();

        // Inject json converters.
        builder.Services.AddSingleton<JsonSettingsService>();
		builder.Services.AddSingleton<TagIdConverter>();
		builder.Services.AddSingleton<GuidTagConverter>();
		builder.Services.AddSingleton<MapTagConverter>();
		builder.Services.AddSingleton<MapTagListConverter>();

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

	private static void InitializeDatabase(WebApplication app)
	{
		using IServiceScope scope = app.Services.CreateScope();
		UserDataContext dbContext = scope.ServiceProvider.GetRequiredService<UserDataContext>();
		// Ensure the folder exists.
		Directory.CreateDirectory(Path.GetDirectoryName(dbContext.Database.GetDbConnection().DataSource)!);

		// Migrations
		dbContext.Database.Migrate();
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

        // Serve static files.
        app.UseStaticFiles(new StaticFileOptions
		{
			ContentTypeProvider = provider
		});

        // Add external maps middleware
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.GetFullPath(app.Configuration["Paths:MapsPath"]!)),
            RequestPath = "/maps",
            ContentTypeProvider = provider
        });

        // Cache static files if not in development environment.
        if (!app.Environment.IsDevelopment())
		{
			app.Use(async (context, next) =>
			{
				context.Response.GetTypedHeaders().CacheControl =
					new Microsoft.Net.Http.Headers.CacheControlHeaderValue
					{
						Public = true,
						MaxAge = TimeSpan.FromMinutes(5)
					};
				await next();
			});
		}

		app.MapControllers();
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
