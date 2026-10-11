using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LocalizationController = JustDanceServer.Controllers.prod_next.just_dance.com.editorial.v1.Localization;
using LocalizationDescriptor = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.localizations.strings.All;

namespace JustDanceServer.Tests.Services;

public sealed class SharedLocalizationTests
{
    internal const string AuthoredText = """
		[
		  {"key":"test.title","translations":{"en-US":"Title","fr":"Titre","fr-CA":"Titre canadien","fr-FR":"Titre français"},
		   "aliases":[{"namespace":"jdnext","id":120001},{"namespace":"jdu","id":220001}]},
		  {"key":"test.separate","translations":{"en-US":"Different meaning"},"aliases":[{"namespace":"jdu","id":120001}]}
		]
		""";

    [Fact]
    public async Task SharedTextKeepsProtocolNamespacesSeparateAndResolvesExactNeutralAndEnglish()
    {
        LocalizedStringService service = await CreateAsync(AuthoredText);
        Assert.Equal("Titre canadien", service.ResolveText("test.title", "fr-CA"));
        Assert.Equal("Titre", service.ResolveText("test.title", "fr-BE"));
        Assert.Equal("Title", service.ResolveText("test.title", "nl-NL"));
        Assert.Equal("Literal", service.ResolveText("missing.optional", "nl-NL", "Literal"));
        Assert.Throws<KeyNotFoundException>(() => service.ResolveText("missing.required", "en-US"));
        Assert.Equal(220001, service.GetJduAlias(120001));
        Assert.Equal(120001, service.GetTextTag("test.title")!.OasisId);
        Assert.Equal("Different meaning", service.GetJduLocalisation()["120001"]["en"]);
    }

    [Fact]
    public async Task JduJsonUsesLowercaseLocaleKeysAndSuppliesTheClientsNeutralFallback()
    {
        LocalizedStringService service = await CreateAsync(AuthoredText);
        JsonElement document = JsonSerializer.SerializeToElement(service.GetJduLocalisation());
        JsonElement translations = document.GetProperty("220001");
        Assert.Equal("Titre français", translations.GetProperty("fr-fr").GetString());
        Assert.Equal("Titre canadien", translations.GetProperty("fr-ca").GetString());
        Assert.Equal("Titre", translations.GetProperty("fr").GetString());
        Assert.Equal("Title", translations.GetProperty("nl").GetString());
        Assert.False(translations.TryGetProperty("fr-FR", out _));
    }

    [Fact]
    public async Task ExistingIdsAndGuidsSurviveTranslationsAndUnrelatedAdditions()
    {
        Guid id = Guid.NewGuid();
        LocalizedStringDatabase legacy = new()
        {
            LocalizedStrings = [new() { OasisId = 120001, LocalizedStringId = id, LocaleCode = "en-US", DisplayString = "Old title" }]
        };
        LocalizedStringService service = await CreateAsync(AuthoredText, legacy);
        LocalizedStringService restarted = await CreateAsync(AuthoredText, legacy);
        restarted.GetAddLocalizedTag("Another title");
        Assert.Equal(id, service.GetTextTag("test.title")!.LocalizedStringId);
        Assert.Equal(service.GetTextTag("test.title"), restarted.GetTextTag("test.title"));
        Assert.Equal(120001, service.GetDatabase("fr-FR").LocalizedStrings.Single(value => value.LocalizedStringId == id).OasisId);
        Assert.Equal("Titre français", service.GetDatabase("fr-FR").LocalizedStrings.Single(value => value.LocalizedStringId == id).DisplayString);
    }

    [Theory]
    [InlineData("[{\"key\":\"x\",\"translations\":{\"fr\":\"Missing fallback\"}}]")]
    [InlineData("[{\"key\":\"x\",\"translations\":{\"en-US\":\"X\"},\"aliases\":[{\"namespace\":\"jdnext\",\"id\":55},{\"namespace\":\"jdnext\",\"id\":55}]}]")]
    [InlineData("[{\"key\":\"x\",\"translations\":{\"en-US\":\"X\",\"en-us\":\"Duplicate\"}}]")]
    [InlineData("[null]")]
    public async Task InvalidSharedDefinitionsFailAtLoad(string json)
        => await Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync(json));

    [Fact]
    public async Task AuthoredTextDoesNotLeakStaleLegacyTranslationsIntoOnlyOneProtocol()
    {
        LocalizedStringDatabase legacy = new()
        {
            LocalizedStrings = [new() { OasisId = 120001, LocaleCode = "de", DisplayString = "Obsolete translation", LocalizedStringId = Guid.NewGuid() }]
        };
        LocalizedStringService service = await CreateAsync(AuthoredText, legacy);
        Assert.Equal("Title", service.ResolveTag(service.GetTextTag("test.title")!, "de-DE"));
        Assert.Equal("Title", service.GetDatabase("de-DE").LocalizedStrings.Single(value => value.OasisId == 120001).DisplayString);
        Assert.Equal("Title", service.GetJduLocalisation()["220001"]["de"]);
    }

    [Fact]
    public async Task EditorialUsesSharedTranslationsAndDoesNotMutateTheEnglishBaseline()
    {
        LocalizedStringService text = await CreateAsync(AuthoredText);
        Mock<IFileSystem> files = new();
        files.Setup(value => value.DirectoryExists("editorial")).Returns(true);
        files.Setup(value => value.GetFiles("editorial", "*.json", SearchOption.TopDirectoryOnly)).Returns(["messages.json"]);
        files.Setup(value => value.OpenRead("messages.json")).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("""
			[{"id":"11111111-1111-1111-1111-111111111111","title":"Fallback","body":"Body",
			  "titleKey":"test.title","bodyKey":"test.separate"}]
			""")));
        EditorialService service = new(Options.Create(new PathSettings { EditorialPath = "editorial" }),
            new JsonSettingsService(Mock.Of<IServiceProvider>()), files.Object, new DiscoveryClock(),
            NullLogger<EditorialService>.Instance, text);
        await service.LoadData();
        EditorialEntry french = Assert.Single(service.GetEntries(new(EditorialProduct.JDUnlimited, Locale: "fr-CA")));
        EditorialEntry english = Assert.Single(service.GetEntries(new(EditorialProduct.JDPlus)));
        Assert.Equal("Titre canadien", french.Title);
        Assert.Equal("Title", english.Title);
        Assert.Equal("Different meaning", french.Body);
        Assert.Equal(english.Id, french.Id);
        Assert.Equal("fr-CA", french.Locale);
        Assert.Equal("en-US", english.Locale);
    }

    [Fact]
    public async Task DefaultAliasesCannotSilentlyReplaceExistingOfficialIds()
    {
        LocalizedStringDatabase legacy = new() { LocalizedStrings = [new(900001, "Existing unrelated text")] };
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync("[]", legacy));
    }

    [Fact]
    public async Task DownloadAndDescriptorAgreeOnLocaleAndActualCompressedChecksum()
    {
        LocalizedStringService service = await CreateAsync(AuthoredText);
        LocalizationController download = new(service);
        FileContentResult file = Assert.IsType<FileContentResult>(download.GetLocalization("fr-CA"));
        LocalizationDescriptor controller = new(new TimingService(), Options.Create(new UrlSettings { CDNUrl = "cdn.example.test/" }),
            service, new DiscoveryClock())
        { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        JsonElement descriptor = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(
            controller.GetAll(JdNextConstants.GameSpaceId, "fr-CA")).Value, JsonSerializerOptions.Web);
        Assert.Equal("fr-CA", descriptor.GetProperty("localeCode").GetString());
        Assert.Equal("https://cdn.example.test/editorial/v1/All.fr-CA.json.gz", descriptor.GetProperty("getUrl").GetString());
        Assert.Equal(Convert.ToHexStringLower(MD5.HashData(file.FileContents)), descriptor.GetProperty("md5").GetString());
        Assert.Equal(file.FileContents, Assert.IsType<FileContentResult>(download.GetLocalization("fr-CA")).FileContents);
        using GZipStream gzip = new(new MemoryStream(file.FileContents), CompressionMode.Decompress);
        using JsonDocument json = await JsonDocument.ParseAsync(gzip);
        Assert.Contains(json.RootElement.GetProperty("localizedStrings").EnumerateArray(),
            value => value.GetProperty("displayString").GetString() == "Titre canadien");
    }

    internal static async Task<LocalizedStringService> CreateAsync(string authored = "[]", LocalizedStringDatabase? legacy = null)
    {
        Mock<IFileSystem> files = new();
        string root = Path.Combine(Path.GetTempPath(), "shared-localization-tests");
        Dictionary<string, string> documents = new() { [Path.Combine(root, "shared-text.json")] = authored };
        if (legacy is not null)
            documents[Path.Combine(root, "localizedstrings.json")] = JsonSerializer.Serialize(legacy);
        files.Setup(value => value.FileExists(It.IsAny<string>())).Returns((string path) => documents.ContainsKey(path));
        files.Setup(value => value.OpenRead(It.IsAny<string>())).Returns((string path) => new MemoryStream(Encoding.UTF8.GetBytes(documents[path])));
        Mock<IServiceProvider> provider = new();
        provider.Setup(value => value.GetService(typeof(IOptions<PathSettings>))).Returns(Options.Create(new PathSettings { JsonsPath = root }));
        provider.Setup(value => value.GetService(typeof(JsonSettingsService))).Returns(new JsonSettingsService(Mock.Of<IServiceProvider>()));
        LocalizedStringService service = new(NullLogger<LocalizedStringService>.Instance, provider.Object, files.Object);
        await service.LoadData();
        return service;
    }
}
