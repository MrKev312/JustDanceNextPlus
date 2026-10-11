using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Utilities;

public sealed class JsonInt64Converter : JsonConverter<long>
{
	public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// Accept decimal and exponent notation without rounding fractional values.
		if (reader.TokenType == JsonTokenType.Number
			&& long.TryParse(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan,
				NumberStyles.Float, CultureInfo.InvariantCulture, out long value))
			return value;

		if (reader.TokenType == JsonTokenType.String
			&& options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString)
			&& long.TryParse(reader.GetString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
			return value;

		throw new JsonException("Expected a whole number within the Int64 range.");
	}

	public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value);
	}
}
