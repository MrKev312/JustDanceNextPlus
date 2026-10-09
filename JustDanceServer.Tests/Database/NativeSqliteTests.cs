using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace JustDanceServer.Tests.Database;

public sealed class NativeSqliteTests(ITestOutputHelper output)
{
    [Fact]
    public async Task LoadedNativeEngineIncludesThe3502SecurityFix()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version()";
        string version = Assert.IsType<string>(await command.ExecuteScalarAsync());
        output.WriteLine("Loaded SQLite engine: " + version);
        Assert.True(Version.Parse(version) >= new Version(3, 50, 2), version);
    }
}
