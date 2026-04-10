using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace JustDanceNextPlus.Services;

public interface IPairingService
{
	PairingEntry CreatePairing(string ipAddress, int port, string pkcs12Certificate, string tlsCertificate, string protocol, string titleId, string displayName = "JustDanceNextPlus", int platform = 38);
	PairingEntry? GetPairingInfo(string code);
	PairingEntry? AddPhone(string code, PairedPhone phone);
	bool RemovePhone(string code, string deviceId);
	bool RemovePairing(string code);
}

public class PairingService : IPairingService
{
	readonly ConcurrentDictionary<string, PairingEntry> pairings = new();
	readonly ConcurrentDictionary<string, Timer> expirationTimers = new();

	private const int PairingExpirationMinutes = 10;
	private const int CodeLength = 8;
	private const string CodeChars = "0123456789";

	public PairingEntry CreatePairing(string ipAddress, int port, string pkcs12Certificate, string tlsCertificate, string protocol, string titleId, string displayName = "JustDanceNextPlus", int platform = 38)
	{
		string code = GenerateCode();

		PairingEntry entry = new()
		{
			PairingCode = code,
			DeviceIp = ipAddress,
			DevicePort = port,
			Pkcs12Certificate = pkcs12Certificate,
			TlsCertificate = tlsCertificate,
			Protocol = protocol,
			TitleId = titleId,
			DisplayName = displayName,
			Platform = platform
		};

		pairings[code] = entry;
		StartExpirationTimer(code);

		return entry;
	}

	public PairingEntry? GetPairingInfo(string code)
	{
		pairings.TryGetValue(code, out PairingEntry? entry);
		return entry;
	}

	public PairingEntry? AddPhone(string code, PairedPhone phone)
	{
		if (!pairings.TryGetValue(code, out PairingEntry? entry))
			return null;

		PairingEntry updated = entry with
		{
			PairedPhones = [.. entry.PairedPhones, phone]
		};

		pairings[code] = updated;
		return updated;
	}

	public bool RemovePhone(string code, string deviceId)
	{
		if (!pairings.TryGetValue(code, out PairingEntry? entry))
			return false;

		PairingEntry updated = entry with
		{
			PairedPhones = [.. entry.PairedPhones.Where(p => p.DeviceId != deviceId)]
		};

		pairings[code] = updated;
		return true;
	}

	public bool RemovePairing(string code)
	{
		if (expirationTimers.TryRemove(code, out Timer? timer))
			timer.Dispose();

		return pairings.TryRemove(code, out _);
	}

	string GenerateCode()
	{
		Span<char> code = stackalloc char[CodeLength];

		do
		{
			for (int i = 0; i < CodeLength; i++)
				code[i] = CodeChars[RandomNumberGenerator.GetInt32(CodeChars.Length)];
		}
		while (pairings.ContainsKey(new string(code)));

		return new string(code);
	}

	void StartExpirationTimer(string code)
	{
		Timer timer = new(_ => RemovePairing(code), null,
			TimeSpan.FromMinutes(PairingExpirationMinutes), Timeout.InfiniteTimeSpan);

		if (expirationTimers.TryRemove(code, out Timer? old))
			old.Dispose();

		expirationTimers[code] = timer;
	}
}

public record PairingEntry
{
	public required string PairingCode { get; init; }
	public required string DeviceIp { get; init; }
	public required int DevicePort { get; init; }
	public required string TlsCertificate { get; init; }
	public string Protocol { get; init; } = "keel";
	public string TitleId { get; init; } = "";
	public string Pkcs12Certificate { get; init; } = "";
	public string DisplayName { get; init; } = "JustDanceNextPlus";
	public int Platform { get; init; } = 38;
	public List<PairedPhone> PairedPhones { get; init; } = [];
}

public record PairedPhone
{
	public required string Language { get; init; }
	public required string DeviceId { get; init; }
	public required string Pkcs12Certificate { get; init; }
	public int Platform { get; init; } = 11;
}
