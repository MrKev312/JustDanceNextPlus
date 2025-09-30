using JustDanceNextPlus.Utilities;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Services;

public class JsonSettingsService
{
	public JsonSettingsService(IServiceProvider serviceProvider)
	{
		// Get the JsonConverters
		GuidTagConverter guidTagConverter = serviceProvider.GetRequiredService<GuidTagConverter>();
		MapTagConverter mapTagConverter = serviceProvider.GetRequiredService<MapTagConverter>();
		TagIdConverter tagIdConverter = serviceProvider.GetRequiredService<TagIdConverter>();
		MapTagListConverter mapTagListConverter = serviceProvider.GetRequiredService<MapTagListConverter>();

		// Add the JsonConverters to the options
		AddConverter(
			[guidTagConverter, mapTagConverter, tagIdConverter, mapTagListConverter, 
			new CategoryConverter()]);
	}

	private void AddConverter(params JsonConverter[] converters)
	{
		foreach (JsonConverter converter in converters)
		{
			PrettyPascalFormat.Converters.Add(converter);
			ShortFormat.Converters.Add(converter);
		}
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
