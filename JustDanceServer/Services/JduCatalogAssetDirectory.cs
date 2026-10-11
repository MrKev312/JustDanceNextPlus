using JustDanceServer.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Services;

internal sealed class JduCatalogAssetDirectory(PathSettings paths)
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly string root = GetConfiguredRoot(paths);

    public string GetFamilyRoot(string family)
    {
        string path = Path.Combine(root, family);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"JDU catalog asset directory '{path}' does not exist.");
        if (Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).FirstOrDefault() is string file)
            throw new InvalidDataException($"JDU catalog asset directory '{path}' contains ungrouped file '{file}'.");
        return path;
    }

    public static string[] GetGroups(string familyRoot)
    {
        string[] groups = [.. Directory
            .EnumerateDirectories(familyRoot, "*", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)];
        if (groups.Length == 0)
            throw new InvalidDataException($"JDU catalog asset directory '{familyRoot}' contains no groups.");

        IGrouping<string, string>? duplicate = groups
            .GroupBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidDataException($"Duplicate JDU catalog group name '{duplicate.Key}'.");
        return groups;
    }

    public static async Task<T> LoadManifest<T>(string group, string fileName)
    {
        string[] manifests = [.. Directory.EnumerateFiles(group, fileName, SearchOption.TopDirectoryOnly)];
        if (manifests.Length != 1)
            throw new InvalidDataException($"JDU catalog group '{group}' must contain exactly one {fileName}.");

        await using FileStream stream = File.OpenRead(manifests[0]);
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions)
            ?? throw new InvalidDataException($"JDU catalog manifest '{manifests[0]}' contains null.");
    }

    public static string GetRequiredFile(string group, string fileName)
    {
        string path = Path.Combine(group, fileName);
        return File.Exists(path)
            ? Path.GetFullPath(path)
            : throw new FileNotFoundException($"JDU catalog group '{group}' is missing '{fileName}'.", path);
    }

    public static string? GetOptionalFile(string group, string fileName)
    {
        string path = Path.Combine(group, fileName);
        return File.Exists(path) ? Path.GetFullPath(path) : null;
    }

    public static void RejectUnexpectedFiles(string group, IEnumerable<string> expectedFiles)
    {
        HashSet<string> expected = expectedFiles
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string? unexpected = Directory
            .EnumerateFiles(group, "*", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .FirstOrDefault(file => !expected.Contains(file));
        if (unexpected is not null)
            throw new InvalidDataException($"JDU catalog group '{group}' contains unexpected file '{unexpected}'.");
    }

    public string GetRelativePath(string fullPath)
    {
        string relative = Path.GetRelativePath(root, Path.GetFullPath(fullPath));
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new InvalidDataException($"JDU catalog asset '{fullPath}' escapes '{root}'.");
        return relative.Replace('\\', '/');
    }

    private static string GetConfiguredRoot(PathSettings paths)
    {
        if (string.IsNullOrWhiteSpace(paths.JduCatalogAssetsPath))
            throw new InvalidDataException("Paths:JduCatalogAssetsPath is required for JDU catalogs.");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(paths.JduCatalogAssetsPath));
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
