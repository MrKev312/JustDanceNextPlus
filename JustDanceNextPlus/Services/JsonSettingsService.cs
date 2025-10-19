using JustDanceNextPlus.Utilities;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Services;

public class JsonSettingsService
{
	public JsonSettingsService(IServiceProvider serviceProvider)
	{
		Type[] serviceConvertsers = [
			typeof(GuidTagConverter),
			typeof(MapTagConverter),
			typeof(TagIdConverter),
			typeof(MapTagListConverter)
            ];

		List<JsonConverter> converters = [ new CategoryConverter() ];

        // Foreach check if it's registered
        foreach (Type converterType in serviceConvertsers)
		{
			if (serviceProvider.GetService(converterType) is JsonConverter converter)
			{
				converters.Add(converter);
			}
            // Else we're in a test environment
        }

        // Add the JsonConverters to the options
        AddConverter(converters);
	}

	private void AddConverter(IEnumerable<JsonConverter> converters)
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
