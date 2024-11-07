using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.GraphQL;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

		app.Run();
	}

	private static void ConfigureServices(WebApplicationBuilder builder)
	{
		// Add controllers
		builder.Services.AddControllers();

		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

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

		// Configure Entity Framework with SQLite.
		builder.Services.AddDbContext<UserDataContext>(options =>
			options.UseLazyLoadingProxies()
				   .UseSqlite($"Data Source={builder.Configuration["Paths:UserDataPath"]}"));

		// Inject custom services.
		builder.Services.AddScoped<UserDataService>();
		builder.Services.AddSingleton<LocalizedStringService>();
		builder.Services.AddSingleton<TagService>();
		builder.Services.AddSingleton<MapService>();
		builder.Services.AddSingleton<PartyManager>();
		builder.Services.AddSingleton<SecurityService>();
		builder.Services.AddSingleton<SessionManager>();
		builder.Services.AddSingleton<TimingService>();
		builder.Services.AddSingleton<UtilityService>();

		// Inject json converters.
		builder.Services.AddSingleton<TagIdConverter>();
		builder.Services.AddSingleton<GuidTagConverter>();

		// Add GraphQL server.
		builder.Services.AddGraphQLServer()
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
		builder.Services.AddMvc()
			.AddJsonOptions(options =>
			{
				var serviceProvider = builder.Services.BuildServiceProvider();
				var oasisTagConverter = serviceProvider.GetRequiredService<TagIdConverter>();
				var guidTagConverter = serviceProvider.GetRequiredService<GuidTagConverter>();

				options.JsonSerializerOptions.Converters.Add(oasisTagConverter);
				options.JsonSerializerOptions.Converters.Add(guidTagConverter);

				options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
				options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
				options.JsonSerializerOptions.WriteIndented = true;
			});



		// Update the JsonSettings for the pretty format.
		var serviceProvider = builder.Services.BuildServiceProvider();
		var oasisTagConverter = serviceProvider.GetRequiredService<TagIdConverter>();
		var guidTagConverter = serviceProvider.GetRequiredService<GuidTagConverter>();

		JsonSettings.PrettyPascalFormat = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			Converters = { oasisTagConverter, guidTagConverter }
		};
	}

	private static void InitializeDatabase(WebApplication app)
	{
		using IServiceScope scope = app.Services.CreateScope();
		UserDataContext dbContext = scope.ServiceProvider.GetRequiredService<UserDataContext>();
		// Ensure the folder exists.
		Directory.CreateDirectory(Path.GetDirectoryName(dbContext.Database.GetDbConnection().DataSource)!);
		dbContext.Database.EnsureCreated();
	}

	private static void ConfigureMiddleware(WebApplication app)
	{
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();
		app.UseResponseCompression();

		// Configure static file serving with custom content types.
		FileExtensionContentTypeProvider provider = new();
		provider.Mappings[".bundle"] = "application/octet-stream";
		provider.Mappings[".webm"] = "video/webm";
		provider.Mappings[".opus"] = "audio/opus";

		app.UseStaticFiles(new StaticFileOptions
		{
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
		app.MapGraphQL("/profile/v3/graphql");
	}

	public static bool CheckJsonExistance(string jsonPath)
	{
		string[] paths = [
			"activity-page.json",
			"parameters.json",
			"parametersv1.json",
			"playlistdb.json",
			"shop-config.json",
			"tagdb.json"
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

public static class JsonSettings
{
	public static JsonSerializerOptions PrettyPascalFormat { get; set; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public static JsonSerializerOptions ShortFormat { get; set; } = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};
}

public class TimingService
{
	public string TimeString(DateTime time)
	{
		return time.ToString("yyyy-MM-ddTHH:mm:ssZ");
	}

	public string ServerTime()
	{
		return TimeString(DateTime.UtcNow);
	}
}
