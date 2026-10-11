using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using JustDanceServer.Configuration;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Tests.Configuration;

public sealed class PemCertificateConfigurationTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"JustDanceServer-PemCertificate-{Guid.NewGuid():N}");

    [Fact]
    public void Configure_AllowsTheEntireCertificateSectionToBeAbsent()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        PemCertificateConfiguration.Configure(builder);
    }

    [Fact]
    public void Configure_AllowsTheCertificateDirectoryToBeBlank()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{PemCertificateConfiguration.SectionName}:Directory"] = string.Empty
        });

        PemCertificateConfiguration.Configure(builder);
    }

    [Fact]
    public void Configure_AllowsAConfiguredCertificateDirectoryNotToExist()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{PemCertificateConfiguration.SectionName}:Directory"] = directory
        });

        PemCertificateConfiguration.Configure(builder);
    }

    [Fact]
    public void Load_ReadsCertificatePrivateKeyAndChainDirectlyFromPemFiles()
    {
        WriteCertificateFiles(out string leafThumbprint, out string issuerThumbprint);

        LoadedPemCertificate loaded = PemCertificateConfiguration.Load(
            new PemCertificateSettings { Directory = directory },
            AppContext.BaseDirectory);

        using (loaded.Certificate)
        {
            Assert.True(loaded.Certificate.HasPrivateKey);
            Assert.Equal(leafThumbprint, loaded.Certificate.Thumbprint);
            Assert.Single(loaded.Chain);
            Assert.Equal(issuerThumbprint, loaded.Chain[0].Thumbprint);
        }

        foreach (X509Certificate2 chainCertificate in loaded.Chain)
            chainCertificate.Dispose();
    }

    [Fact]
    public async Task Configure_SuppliesThePemLeafAndChainToKestrel()
    {
        WriteCertificateFiles(out string leafThumbprint, out string issuerThumbprint);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{PemCertificateConfiguration.SectionName}:Directory"] = directory
        });
        PemCertificateConfiguration.Configure(builder);
        HttpsConnectionAdapterOptions? httpsOptions = null;
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(
            IPAddress.Loopback,
            0,
            listen => listen.UseHttps(options => httpsOptions = options)));

        await using WebApplication app = builder.Build();
        try
        {
            _ = app.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;
            Assert.NotNull(httpsOptions);
            Assert.NotNull(httpsOptions.ServerCertificate);
            Assert.True(httpsOptions.ServerCertificate.HasPrivateKey);
            Assert.Equal(leafThumbprint, httpsOptions.ServerCertificate.Thumbprint);
            Assert.NotNull(httpsOptions.ServerCertificateChain);
            Assert.Single(httpsOptions.ServerCertificateChain);
            Assert.Equal(issuerThumbprint, httpsOptions.ServerCertificateChain[0].Thumbprint);

            // Trust only the generated issuer, without using system trust or certificate downloads.
            using X509Chain chain = new();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(httpsOptions.ServerCertificateChain[0]);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.DisableCertificateDownloads = true;
            Assert.True(chain.Build(httpsOptions.ServerCertificate));
            Assert.Equal(2, chain.ChainElements.Count);
            Assert.Equal(issuerThumbprint, chain.ChainElements[1].Certificate.Thumbprint);
        }
        finally
        {
            httpsOptions?.ServerCertificate?.Dispose();
            if (httpsOptions?.ServerCertificateChain is not null)
                foreach (X509Certificate2 chainCertificate in httpsOptions.ServerCertificateChain)
                    chainCertificate.Dispose();
        }
    }

    [Fact]
    public void Load_FailsClearlyWhenARequiredPemFileIsMissing()
    {
        Directory.CreateDirectory(directory);

        FileNotFoundException exception = Assert.Throws<FileNotFoundException>(() =>
            PemCertificateConfiguration.Load(
                new PemCertificateSettings { Directory = directory },
                AppContext.BaseDirectory));

        Assert.Contains("certificate PEM file is missing", exception.Message);
        Assert.EndsWith("cert.pem", exception.FileName, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private void WriteCertificateFiles(out string leafThumbprint, out string issuerThumbprint)
    {
        Directory.CreateDirectory(directory);
        using RSA leafKey = RSA.Create(2048);
        using RSA issuerKey = RSA.Create(2048);
        CertificateRequest issuerRequest = new(
            "CN=Test issuer",
            issuerKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        issuerRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        issuerRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
            true));
        issuerRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(issuerRequest.PublicKey, false));
        using X509Certificate2 issuer = issuerRequest.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));

        CertificateRequest leafRequest = new(
            "CN=*.ubi.com",
            leafKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        leafRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            true));
        leafRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [new Oid("1.3.6.1.5.5.7.3.1")],
            false));
        byte[] serial = RandomNumberGenerator.GetBytes(16);
        using X509Certificate2 leaf = leafRequest.Create(
            issuer,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(7),
            serial);

        leafThumbprint = leaf.Thumbprint;
        issuerThumbprint = issuer.Thumbprint;
        File.WriteAllText(Path.Combine(directory, "cert.pem"), leaf.ExportCertificatePem());
        File.WriteAllText(Path.Combine(directory, "key.pem"), leafKey.ExportPkcs8PrivateKeyPem());
        File.WriteAllText(Path.Combine(directory, "ca_bundle.pem"), issuer.ExportCertificatePem());
    }
}
