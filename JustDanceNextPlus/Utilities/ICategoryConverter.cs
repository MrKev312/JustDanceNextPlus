using JustDanceNextPlus.JustDanceClasses.Database;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public class ICategoryConverter : JsonConverter<ICategory>
{
	public override ICategory? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		JsonElement jsonElement = JsonElement.ParseValue(ref reader);

		if (!jsonElement.TryGetProperty("type", out JsonElement typeElement) || typeElement.GetString() is not { } type)
			throw new JsonException("JSON object is missing a 'type' property or it is not a string.");

		return type switch
		{
			"banner" => jsonElement.Deserialize<BannerCategory>(options),
			"carousel" => jsonElement.Deserialize<CarouselCategory>(options),
			_ => throw new JsonException($"Unknown category type: {type}")
		};
	}

	public override void Write(Utf8JsonWriter writer, ICategory value, JsonSerializerOptions options)
	{
		switch (value)
		{
			case BannerCategory banner:
				JsonSerializer.Serialize(writer, banner, options);
				break;
			case CarouselCategory carousel:
				JsonSerializer.Serialize(writer, carousel, options);
				break;
			default:
				throw new NotSupportedException($"Type {value.GetType()} is not supported by the CategoryConverter.");
		}
	}
}