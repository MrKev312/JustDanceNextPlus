using JustDanceServer.Configuration;

using Microsoft.Extensions.Options;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Services;

[JsonConverter(typeof(JsonStringEnumConverter<EditorialProduct>))]
public enum EditorialProduct
{
	JDPlus,
	JDUnlimited
}

public sealed record EditorialAudience(
	EditorialProduct Product,
	int? GameYear = null,
	string? Platform = null,
	string Locale = "en-US");

public sealed record EditorialEntry
{
	public required Guid Id { get; init; }
	public required string Title { get; init; }
	public required string Body { get; init; }
	public string Summary { get; init; } = string.Empty;
	public string Type { get; init; } = "normal";
	public string Placement { get; init; } = "Notification center";
	public string Locale { get; init; } = "en-US";
	public string? TitleKey { get; init; }
	public string? BodyKey { get; init; }
	public string? SummaryKey { get; init; }
	public string ImageUrl { get; init; } = string.Empty;
	public int Priority { get; init; } = 1;
	public DateTimeOffset PublicationDate { get; init; } = DateTimeOffset.UnixEpoch;
	public DateTimeOffset? ExpirationDate { get; init; }
	public EditorialProduct[] Products { get; init; } =
		[EditorialProduct.JDPlus, EditorialProduct.JDUnlimited];
	public int[] GameYears { get; init; } = [];
	public string[] Platforms { get; init; } = [];
	public bool ShowOnHome { get; init; } = true;
}

public interface IEditorialService : ILoadService
{
	IReadOnlyList<EditorialEntry> GetEntries(EditorialAudience audience);
}

public sealed class EditorialService(
	IOptions<PathSettings> pathSettings,
	JsonSettingsService jsonSettings,
	IFileSystem fileSystem,
	TimeProvider timeProvider,
	ILogger<EditorialService> logger,
	ILocalizedStringService text) : IEditorialService
{
	private IReadOnlyList<EditorialEntry> entries = [];

	public async Task LoadData()
	{
		string path = pathSettings.Value.EditorialPath;
		if (string.IsNullOrWhiteSpace(path))
		{
			entries = [];
			return;
		}

		List<EditorialEntry> loaded = [];
		IEnumerable<string> files = fileSystem.DirectoryExists(path)
			? fileSystem.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly)
				.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
			: Array.Empty<string>();
		foreach (string file in files)
		{
			try
			{
				using Stream stream = fileSystem.OpenRead(file);
				EditorialEntry[] values = await JsonSerializer.DeserializeAsync<EditorialEntry[]>(
					stream,
					jsonSettings.PrettyPascalFormat) ?? [];
				loaded.AddRange(values);
			}
			catch (Exception exception)
			{
				throw new InvalidOperationException($"Failed to load editorial file '{file}'.", exception);
			}
		}

		Guid? duplicate = loaded.GroupBy(value => value.Id)
			.FirstOrDefault(group => group.Count() > 1)?.Key;
		if (duplicate.HasValue)
			throw new InvalidOperationException($"Duplicate editorial ID '{duplicate}'.");
		if (loaded.Any(value => value.Id == Guid.Empty
			|| string.IsNullOrWhiteSpace(value.Title)
			|| string.IsNullOrWhiteSpace(value.Body)))
			throw new InvalidOperationException("Editorial entries require a GUID, title, and body.");
		foreach (string key in loaded.SelectMany(value => new[] { value.TitleKey, value.BodyKey, value.SummaryKey }).OfType<string>())
			_ = text.ResolveText(key, "en-US");

		entries = [.. loaded
			.OrderBy(value => value.Priority)
			.ThenByDescending(value => value.PublicationDate)
			.ThenBy(value => value.Id)];
		logger.LogInformation("Loaded {Count} editorial entries.", entries.Count);
	}

	public IReadOnlyList<EditorialEntry> GetEntries(EditorialAudience audience)
	{
		DateTimeOffset now = timeProvider.GetUtcNow();
		string platform = audience.Platform?.Trim() ?? string.Empty;
		return [.. entries.Where(entry =>
			entry.Products.Contains(audience.Product)
			&& entry.PublicationDate <= now
			&& (entry.ExpirationDate is null || entry.ExpirationDate > now)
			&& (entry.GameYears.Length == 0
				|| (audience.GameYear.HasValue && entry.GameYears.Contains(audience.GameYear.Value)))
			&& (entry.Platforms.Length == 0
				|| entry.Platforms.Contains(platform, StringComparer.OrdinalIgnoreCase)))
			.Where(entry => entry.TitleKey is not null || LocaleFallback.GetCandidates(audience.Locale)
				.Contains(entry.Locale, StringComparer.OrdinalIgnoreCase))
			.Select(entry => entry with
			{
				Title = entry.TitleKey is null ? entry.Title : text.ResolveText(entry.TitleKey, audience.Locale, entry.Title),
				Body = entry.BodyKey is null ? entry.Body : text.ResolveText(entry.BodyKey, audience.Locale, entry.Body),
				Summary = entry.SummaryKey is null ? entry.Summary : text.ResolveText(entry.SummaryKey, audience.Locale, entry.Summary),
				Locale = entry.TitleKey is null ? entry.Locale : LocaleFallback.Normalize(audience.Locale)
			})];
	}
}

public static class EditorialAudienceResolver
{
	public static EditorialAudience FromRequest(HttpRequest request)
	{
		string platform = RequestSessionResolver.GetPlatform(request);
		Guid applicationId = Guid.TryParse(request.Headers["Ubi-AppId"].FirstOrDefault(), out Guid parsedApplicationId)
			? parsedApplicationId : Guid.Empty;
		int? gameYear = WdfRequestResolver.TryGetGameYear(request, out int resolvedYear)
			? resolvedYear
			: null;
		bool knownApplication = applicationId != Guid.Empty
			&& UbisoftApplicationProfiles.TryGetSessionIdentity(applicationId, out _);
		bool isJdNextApplication = UbisoftApplicationProfiles.TryGetApplicationTarget(
			applicationId,
			out UbisoftApplicationTarget? target)
			&& target.SpaceId == JdNextConstants.GameSpaceId;
		bool knownJduApplication = knownApplication && !isJdNextApplication;
		EditorialProduct product = gameYear.HasValue || knownJduApplication
			? EditorialProduct.JDUnlimited
			: EditorialProduct.JDPlus;
		return new EditorialAudience(product, gameYear, platform,
			LocaleFallback.Normalize(request.Headers.AcceptLanguage.FirstOrDefault()));
	}
}
