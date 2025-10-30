using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public class JsonStringConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => true;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
		Type inner = typeof(InnerConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(inner)!;
    }

    private class InnerConverter<T> : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = reader.GetString();
            if (json is null)
                return default;

			JsonSerializerOptions innerOptions = new JsonSerializerOptions(options)
            {
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };

            return JsonSerializer.Deserialize<T>(json, innerOptions);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializerOptions innerOptions = new JsonSerializerOptions(options)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };

            var json = JsonSerializer.Serialize(value, innerOptions);
            writer.WriteStringValue(json);
        }
    }
}
