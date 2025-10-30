using JustDanceNextPlus.Utilities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Text.Encodings.Web;
using System.Text.Json;

namespace JustDanceNextPlus.Configuration;

public class ConfigureJsonOptions(
	TagIdConverter tagIdConverter,
	GuidTagConverter guidTagConverter,
	MapTagConverter mapTagConverter,
	MapTagListConverter mapTagListConverter) : IConfigureOptions<JsonOptions>
{
	public void Configure(JsonOptions options)
	{
		options.JsonSerializerOptions.Converters.Add(tagIdConverter);
		options.JsonSerializerOptions.Converters.Add(guidTagConverter);
		options.JsonSerializerOptions.Converters.Add(mapTagConverter);
		options.JsonSerializerOptions.Converters.Add(mapTagListConverter);

		options.JsonSerializerOptions.Converters.Add(new ICategoryConverter());

		options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
		options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
		options.JsonSerializerOptions.WriteIndented = true;
	}
}