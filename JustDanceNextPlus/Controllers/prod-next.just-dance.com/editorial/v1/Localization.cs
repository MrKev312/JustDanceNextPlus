using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Net.Http.Headers;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/All.en-US.json.gz")]
public class Localization(LocalizedStringService localizedStringService) : ControllerBase
{
	readonly JsonSerializerOptions options = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	[HttpGet]
	[OutputCache(Duration = 36000)]
	public IActionResult GetLocalization()
	{
		// Serialize the LocalizedStringService object to JSON
		string json = JsonSerializer.Serialize(localizedStringService.Database, options);

		// Compress the JSON string into GZip format
		using MemoryStream memoryStream = new();
		using (GZipStream gzipStream = new(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
		using (StreamWriter writer = new(gzipStream))
		{
			writer.Write(json);
		}

		memoryStream.Position = 0;

		return File(memoryStream.ToArray(), "application/x-gzip");
	}
}
