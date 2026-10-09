using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database.Profile;

using System.Globalization;

namespace JustDanceServer.Services;

public sealed record PlayerRequestContext(
	Guid? ProfileId,
	ProgressionFamily Family,
	int? GameYear,
	string Platform,
	string Locale)
{
	public bool SupportsFavoriteMutations => Family == ProgressionFamily.JDUnlimited && GameYear == 2017;
	public bool SupportsMapHistory => Family == ProgressionFamily.JDUnlimited && GameYear is >= 2019 and <= 2022;
}

public static class LocaleFallback
{
	public static string Normalize(string? locale)
	{
		string value = locale?.Split(',', ';')[0].Trim().Replace('_', '-') ?? string.Empty;
		try
		{
			return string.IsNullOrWhiteSpace(value) ? "en-US" : CultureInfo.GetCultureInfo(value).Name;
		}
		catch (CultureNotFoundException)
		{
			return "en-US";
		}
	}

	public static IEnumerable<string> GetCandidates(string? locale)
	{
		string normalized = Normalize(locale);
		return new[] { normalized, normalized.Split('-')[0], "en-US", "en" }
			.Distinct(StringComparer.OrdinalIgnoreCase);
	}

	public static string Resolve(IReadOnlyDictionary<string, string> translations, string? locale, string fallback)
	{
		foreach (string candidate in GetCandidates(locale))
		{
			foreach ((string language, string text) in translations)
			{
				if (string.Equals(language, candidate, StringComparison.OrdinalIgnoreCase))
					return text;
			}
		}

		return fallback;
	}
}
