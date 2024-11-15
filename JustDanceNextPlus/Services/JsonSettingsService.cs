using JustDanceNextPlus.Utilities;

using System.Text.Encodings.Web;
using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class JsonSettingsService
{
	public JsonSettingsService(IServiceProvider serviceProvider)
	{
		// Get the JsonConverters
		GuidTagConverter guidTagConverter = serviceProvider.GetRequiredService<GuidTagConverter>();
		MapTagConverter mapTagConverter = serviceProvider.GetRequiredService<MapTagConverter>();
		TagIdConverter tagIdConverter = serviceProvider.GetRequiredService<TagIdConverter>();

		// Add the JsonConverters to the options
		PrettyPascalFormat.Converters.Add(guidTagConverter);
		PrettyPascalFormat.Converters.Add(mapTagConverter);
		PrettyPascalFormat.Converters.Add(tagIdConverter);

		ShortFormat.Converters.Add(guidTagConverter);
		ShortFormat.Converters.Add(mapTagConverter);
		ShortFormat.Converters.Add(tagIdConverter);
	}

	public JsonSerializerOptions PrettyPascalFormat { get; set; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};
	public JsonSerializerOptions ShortFormat { get; set; } = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};
}
