using JustDanceServer.Configuration;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceServer.Services;

public interface ILocalizedStringService : ILoadService
{
    LocalizedStringDatabase Database { get; }

    LocalizedString? GetLocalizedTag(string text);
    LocalizedString? GetLocalizedTag(Guid id);
    LocalizedString? GetLocalizedTag(int id);
    LocalizedString GetAddLocalizedTag(string text);
    string ResolveText(string key, string? locale, string? fallback = null);
    LocalizedString? GetTextTag(string key);
    string ResolveTag(LocalizedString tag, string? locale);
    IReadOnlyDictionary<string, Dictionary<string, string>> GetJduLocalisation();
    int? GetJduAlias(int jdNextId);
    LocalizedStringDatabase GetDatabase(string? locale);
}

public partial class LocalizedStringService(ILogger<LocalizedStringService> logger,
    IServiceProvider serviceProvider,
    IFileSystem fileSystem) : ILocalizedStringService, ILoadService
{
    public LocalizedStringDatabase Database { get; private set; } = new();
    private readonly Lock stringLock = new();

    // Localization strings that are only available in the game and are not needed to be localized
    // TODO: Add all missing localized strings from the game
    static readonly List<LocalizedString> defaultLocalizedStrings =
    [
        new LocalizedString(0, ""),
        new LocalizedString(1, "OK"),
        new LocalizedString(2, "Yes"),
        new LocalizedString(3, "Continue"),
        new LocalizedString(4, "Back"),
        new LocalizedString(23, "Playlists we think you'll like"),
        new LocalizedString(25, "Songs you might like"),
        new LocalizedString(26, "Everybody loves [VAR:TAGNAME]"),
        new LocalizedString(28, "Everybody's playing them"),
        new LocalizedString(30, "Sweat Longer"),
        new LocalizedString(32, "No"),
        new LocalizedString(33, "Cancel"),
        new LocalizedString(8747, "Just\u00A0Dance+")
    ];

    public async Task LoadData()
    {
        IOptions<PathSettings> settings = serviceProvider.GetRequiredService<IOptions<PathSettings>>();
        JsonSettingsService jsonSettingsService = serviceProvider.GetRequiredService<JsonSettingsService>();

        string path = Path.Combine(settings.Value.JsonsPath, "localizedstrings.json");
        if (!fileSystem.FileExists(path))
        {
            logger.LogInformation("Localized strings database not found, creating a new one");
            await LoadSharedTextAsync(settings.Value.JsonsPath, jsonSettingsService);
            return;
        }

        using Stream fileStream = fileSystem.OpenRead(path);
        LocalizedStringDatabase? db = await JsonSerializer.DeserializeAsync<LocalizedStringDatabase>(fileStream, jsonSettingsService.PrettyPascalFormat);

        if (db == null)
        {
            logger.LogWarning("Localized strings database could not be loaded");
            await LoadSharedTextAsync(settings.Value.JsonsPath, jsonSettingsService);
            return;
        }

        // Sort the localized strings by OasisIdInt
        db.LocalizedStrings = [.. db.LocalizedStrings.OrderBy(ls => ls.OasisId)];

        Database = db;
        await LoadSharedTextAsync(settings.Value.JsonsPath, jsonSettingsService);
        logger.LogInformation("Localized strings database loaded");
    }

    public LocalizedString? GetLocalizedTag(string text)
    {
        lock (stringLock)
        {
            return Database.LocalizedStrings.FirstOrDefault(ls => ls.DisplayString == text)
                ?? defaultLocalizedStrings.FirstOrDefault(ls => ls.DisplayString == text);
        }
    }

    public LocalizedString? GetLocalizedTag(Guid id)
    {
        lock (stringLock)
        {
            return Database.LocalizedStrings.FirstOrDefault(ls => ls.LocalizedStringId == id)
                ?? defaultLocalizedStrings.FirstOrDefault(ls => ls.LocalizedStringId == id);
        }
    }

    public LocalizedString? GetLocalizedTag(int id)
    {
        lock (stringLock)
        {
            return Database.LocalizedStrings.FirstOrDefault(ls => ls.OasisId == id)
                ?? defaultLocalizedStrings.FirstOrDefault(ls => ls.OasisId == id);
        }
    }

    public LocalizedString GetAddLocalizedTag(string text)
    {
        lock (stringLock)
        {
            LocalizedString? localizedTag = GetLocalizedTag(text);
            if (localizedTag != null)
                return localizedTag;

            Guid guid = StableContentId.Create($"text:{text}");
            int id = 1_000_000_000 + (int)(Convert.ToUInt32(StableContentId.Hash(text)[..8], 16) % 1_000_000_000);
            if (GetLocalizedTag(id) is not null)
                throw new InvalidOperationException($"Generated localization ID collision for '{text}'; assign an explicit shared-text alias.");

            localizedTag = new()
            {
                OasisId = id,
                LocaleCode = "en-US",
                DisplayString = text,
                LocalizedStringId = guid
            };

            Database.LocalizedStrings.Add(localizedTag);

            return localizedTag;
        }
    }
}

public class LocalizedStringDatabase
{
    public Guid SpaceId => JdNextConstants.GameSpaceId;
    public List<LocalizedString> LocalizedStrings { get; set; } = [];
}
