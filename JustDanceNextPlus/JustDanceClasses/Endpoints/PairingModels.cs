using JustDanceNextPlus.Services;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public record PairingCodeResponse
{
	public required string PairingCode { get; init; }
	public required string QrCodePairingUrl { get; init; }
}

public record PairingCodeInfoResponse
{
	[JsonPropertyName("deviceIP")]
	public required string DeviceIp { get; init; }
	public required int DevicePort { get; init; }
	public required string PairingCode { get; init; }
	public required string QrCodePairingUrl { get; init; }
	public string Protocol { get; init; } = "keel";
	public string TitleId { get; init; } = "";
	public string TlsCertificate { get; init; } = "";
	public string Pkcs12Certificate { get; init; } = "";
	public string DisplayName { get; init; } = "JustDanceNextPlus";
	public int Platform { get; init; } = 38;
	public List<PairedPhoneResponse> PairedPhones { get; init; } = [];

	public static PairingCodeInfoResponse FromEntry(PairingEntry entry, string hostUrl) => new()
	{
		DeviceIp = entry.DeviceIp,
		DevicePort = entry.DevicePort,
		PairingCode = entry.PairingCode,
		QrCodePairingUrl = $"https://{hostUrl}/pair?code={entry.PairingCode}",
		Protocol = entry.Protocol,
		TitleId = entry.TitleId,
		TlsCertificate = entry.TlsCertificate,
		Pkcs12Certificate = entry.Pkcs12Certificate,
		DisplayName = entry.DisplayName,
		Platform = entry.Platform,
		PairedPhones = [.. entry.PairedPhones.Select(PairedPhoneResponse.FromPhone)]
	};
}

public record PairedPhoneResponse
{
	public required string Language { get; init; }
	public required string DeviceId { get; init; }
	public required string Pkcs12Certificate { get; init; }
	public int Platform { get; init; } = 11;

	public static PairedPhoneResponse FromPhone(PairedPhone phone) => new()
	{
		Language = phone.Language,
		DeviceId = phone.DeviceId,
		Pkcs12Certificate = phone.Pkcs12Certificate,
		Platform = phone.Platform
	};
}
