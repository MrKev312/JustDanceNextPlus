using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Utilities;

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
        private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> ReadOptions = [];
        private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> WriteOptions = [];

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = reader.GetString();
            if (json is null)
                return default;

            JsonSerializerOptions innerOptions = ReadOptions.GetValue(options, static source => new(source)
            {
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            });

            return JsonSerializer.Deserialize<T>(json, innerOptions);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializerOptions innerOptions = WriteOptions.GetValue(options, static source => new(source)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            });

            var json = JsonSerializer.Serialize(value, innerOptions);
            writer.WriteStringValue(json);
        }
    }
}
