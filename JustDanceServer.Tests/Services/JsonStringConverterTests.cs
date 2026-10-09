using JustDanceServer.Utilities;

using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Tests.Services;

public sealed class JsonStringConverterTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Converter_ReusesOptionsWithoutChangingTheCallerConfiguration(bool read)
	{
		RecordingIntConverter recorder = new();
		JsonSerializerOptions options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true,
			Encoder = JavaScriptEncoder.Default,
			Converters = { recorder }
		};
		Envelope envelope = new() { Value = new() { DisplayName = "<dancer>", Score = 42 } };

		for (int iteration = 0; iteration < 3; iteration++)
		{
			if (read)
			{
				Envelope result = JsonSerializer.Deserialize<Envelope>(
					"""{"value":"{\"DISPLAYNAME\":\"<dancer>\",\"SCORE\":42}"}""", options)!;
				Assert.Equal(envelope.Value.DisplayName, result.Value.DisplayName);
				Assert.Equal(envelope.Value.Score, result.Value.Score);
			}
			else
			{
				JsonElement result = JsonSerializer.SerializeToElement(envelope, options);
				Assert.Equal("""{"displayName":"<dancer>","score":42}""", result.GetProperty("value").GetString());
			}
		}

		JsonSerializerOptions[] observed = recorder.Options.ToArray();
		Assert.Equal(3, observed.Length);
		Assert.All(observed, value => Assert.Same(observed[0], value));
		Assert.NotSame(options, observed[0]);
		Assert.False(observed[0].WriteIndented);
		Assert.Equal(read, observed[0].PropertyNameCaseInsensitive);
		Assert.Same(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, observed[0].Encoder);
		Assert.True(options.WriteIndented);
		Assert.False(options.PropertyNameCaseInsensitive);
		Assert.Same(JavaScriptEncoder.Default, options.Encoder);
	}

	[Fact]
	public void Converter_KeepsNamingPoliciesAndCustomConvertersIsolated()
	{
		RecordingIntConverter camelRecorder = new();
		RecordingIntConverter snakeRecorder = new();
		JsonSerializerOptions camelOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Converters = { camelRecorder }
		};
		JsonSerializerOptions snakeOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			Converters = { snakeRecorder }
		};
		Envelope envelope = new() { Value = new() { DisplayName = "Player", Score = 42 } };

		for (int iteration = 0; iteration < 2; iteration++)
		{
			JsonElement camel = JsonSerializer.SerializeToElement(envelope, camelOptions);
			JsonElement snake = JsonSerializer.SerializeToElement(envelope, snakeOptions);
			Assert.Equal("""{"displayName":"Player","score":42}""", camel.GetProperty("value").GetString());
			Assert.Equal("""{"display_name":"Player","score":42}""", snake.GetProperty("value").GetString());
			Assert.Equal(42, camel.Deserialize<Envelope>(camelOptions)!.Value.Score);
			Assert.Equal(42, snake.Deserialize<Envelope>(snakeOptions)!.Value.Score);
		}

		Assert.Equal(4, camelRecorder.Options.Count);
		Assert.Equal(4, snakeRecorder.Options.Count);
		Assert.All(camelRecorder.Options, options => Assert.Same(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy));
		Assert.All(snakeRecorder.Options, options => Assert.Same(JsonNamingPolicy.SnakeCaseLower, options.PropertyNamingPolicy));
	}

	[Fact]
	public async Task Converter_ReusesOptionsAcrossConcurrentWrites()
	{
		RecordingIntConverter recorder = new();
		JsonSerializerOptions options = new() { Converters = { recorder } };
		Envelope envelope = new() { Value = new() { DisplayName = "Player", Score = 42 } };

		string[] results = await Task.WhenAll(Enumerable.Range(0, 16)
			.Select(_ => Task.Run(() => JsonSerializer.Serialize(envelope, options))));

		Assert.All(results, value => Assert.Equal(results[0], value));
		JsonSerializerOptions[] observed = recorder.Options.ToArray();
		Assert.Equal(16, observed.Length);
		Assert.All(observed, value => Assert.Same(observed[0], value));
	}

	private sealed class Envelope
	{
		[JsonConverter(typeof(JsonStringConverter))]
		public Payload Value { get; set; } = new();
	}

	private sealed class Payload
	{
		public string DisplayName { get; set; } = string.Empty;
		public int Score { get; set; }
	}

	private sealed class RecordingIntConverter : JsonConverter<int>
	{
		public ConcurrentQueue<JsonSerializerOptions> Options { get; } = new();

		public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			Options.Enqueue(options);
			return reader.GetInt32();
		}

		public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
		{
			Options.Enqueue(options);
			writer.WriteNumberValue(value);
		}
	}
}
