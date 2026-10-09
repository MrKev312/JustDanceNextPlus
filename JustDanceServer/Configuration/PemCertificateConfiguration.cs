using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace JustDanceServer.Configuration;

public sealed class PemCertificateSettings
{
	public string Directory { get; init; } = string.Empty;
	public string CertificateFile { get; init; } = "cert.pem";
	public string PrivateKeyFile { get; init; } = "key.pem";
	public string ChainFile { get; init; } = "ca_bundle.pem";
}

public sealed record LoadedPemCertificate(
	X509Certificate2 Certificate,
	X509Certificate2Collection Chain);

public static class PemCertificateConfiguration
{
	public const string SectionName = "HttpsCertificate";

	public static void Configure(WebApplicationBuilder builder)
	{
		PemCertificateSettings settings = builder.Configuration
			.GetSection(SectionName)
			.Get<PemCertificateSettings>() ?? new PemCertificateSettings();

		if (string.IsNullOrWhiteSpace(settings.Directory))
			return;

		string directory = ResolvePath(settings.Directory, builder.Environment.ContentRootPath);
		if (!System.IO.Directory.Exists(directory))
			return;

		LoadedPemCertificate loaded = Load(settings, builder.Environment.ContentRootPath);
		builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureHttpsDefaults(https =>
		{
			https.ServerCertificate = loaded.Certificate;
			https.ServerCertificateChain = loaded.Chain;
		}));
	}

	public static LoadedPemCertificate Load(
		PemCertificateSettings settings,
		string contentRootPath)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

		string directory = ResolvePath(settings.Directory, contentRootPath);
		string certificatePath = ResolveFile(directory, settings.CertificateFile, nameof(settings.CertificateFile));
		string privateKeyPath = ResolveFile(directory, settings.PrivateKeyFile, nameof(settings.PrivateKeyFile));
		string chainPath = ResolveFile(directory, settings.ChainFile, nameof(settings.ChainFile));

		RequireFile(certificatePath, "certificate");
		RequireFile(privateKeyPath, "private key");
		RequireFile(chainPath, "certificate chain");

		try
		{
			X509Certificate2 certificate = X509Certificate2.CreateFromPemFile(
				certificatePath,
				privateKeyPath);
			certificate = MakeServerCompatible(certificate);

			if (!certificate.HasPrivateKey)
				throw new InvalidDataException(
					$"PEM certificate '{certificatePath}' did not load with a private key.");

			X509Certificate2Collection chain = [];
			chain.ImportFromPemFile(chainPath);
			if (chain.Count == 0)
				throw new InvalidDataException(
					$"PEM certificate chain '{chainPath}' contains no certificates.");

			return new LoadedPemCertificate(certificate, chain);
		}
		catch (Exception exception) when (exception is not InvalidDataException)
		{
			throw new InvalidDataException(
				$"Failed to load HTTPS certificate PEM files from '{directory}'.",
				exception);
		}
	}

	private static X509Certificate2 MakeServerCompatible(X509Certificate2 certificate)
	{
		if (!OperatingSystem.IsWindows())
			return certificate;

		// Windows Schannel cannot serve a certificate backed by the ephemeral key
		// returned by CreateFromPemFile. Reimport it into the temporary user key
		// store without ever requiring or writing a PFX file.
		byte[] temporaryPkcs12 = certificate.Export(X509ContentType.Pfx);
		certificate.Dispose();
		try
		{
			return X509CertificateLoader.LoadPkcs12(
				temporaryPkcs12,
				password: null,
				X509KeyStorageFlags.UserKeySet);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(temporaryPkcs12);
		}
	}

	private static string ResolveFile(string directory, string file, string settingName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(file, settingName);
		return ResolvePath(file, directory);
	}

	private static string ResolvePath(string path, string relativeTo)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(relativeTo, path));
	}

	private static void RequireFile(string path, string description)
	{
		if (!File.Exists(path))
			throw new FileNotFoundException($"HTTPS {description} PEM file is missing: {path}", path);
	}
}
