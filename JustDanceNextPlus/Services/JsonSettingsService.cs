using JustDanceNextPlus.Utilities;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Services;

public class JsonSettingsService
{
	public JsonSettingsService(IServiceProvider serviceProvider)
	{
		Type[] serviceConverters = [
			typeof(GuidTagConverter),
			typeof(MapTagConverter),
			typeof(TagIdConverter),
			typeof(MapTagListConverter)
            ];

        // Foreach check if it's registered
        foreach (Type converterType in serviceConverters)
		{
			if (serviceProvider.GetService(converterType) is JsonConverter converter)
			{
				PrettyPascalFormat.Converters.Add(converter);
            }
            // Else we're in a test environment
        }
	}

	public JsonSerializerOptions PrettyPascalFormat { get; set; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};
}
