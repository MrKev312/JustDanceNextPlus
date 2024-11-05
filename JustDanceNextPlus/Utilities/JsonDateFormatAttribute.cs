using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

[AttributeUsage(AttributeTargets.Property)]
public class JsonDateFormatAttribute(string dateFormat) : JsonConverterAttribute
{
	public string DateFormat { get; } = dateFormat;

	public override JsonConverter CreateConverter(Type typeToConvert)
	{
		return new JsonDateTimeConverter(DateFormat);
	}
}

public class JsonDateTimeConverter(string dateFormat) : JsonConverter<DateTime>
{
	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType != JsonTokenType.Null
			? DateTime.ParseExact(reader.GetString()!, dateFormat, null) 
			: default;
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString(dateFormat));
	}
}
