using JustDanceServer.JustDanceClasses.Database;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Services;

public interface IDatabaseInitializer
{
	Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class DatabaseInitializer(
	IServiceScopeFactory scopeFactory,
	IMapService mapService,
	ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
	public async Task InitializeAsync(CancellationToken cancellationToken)
	{
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		UserDataContext dbContext = scope.ServiceProvider.GetRequiredService<UserDataContext>();
		Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbContext.Database.GetDbConnection().DataSource))!);

		SqliteConnection connection = (SqliteConnection)dbContext.Database.GetDbConnection();
		bool databaseExisted = File.Exists(connection.DataSource);
		await connection.OpenAsync(cancellationToken);

		try
		{
			string[] pendingMigrations = [.. await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)];
			if (pendingMigrations.Length > 0 && databaseExisted)
			{
				// BackupDatabase includes committed WAL data and must finish before any migration writes.
				string backupPath = BackupDatabase(connection);
				logger.LogInformation("Saved pre-migration database backup to {BackupPath}", backupPath);
			}

			MapIdentityMigrationFunctions.Register(
				connection,
				mapService.Maps.ToDictionary(pair => pair.Key, pair => pair.Value.Song.MapName),
				message => logger.LogWarning("{MigrationWarning}", message));

			await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
			await dbContext.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;", cancellationToken);
			await dbContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;", cancellationToken);
			await dbContext.Database.MigrateAsync(cancellationToken);
		}
		finally
		{
			await connection.CloseAsync();
		}
	}

	private static string BackupDatabase(SqliteConnection source)
	{
		string databasePath = Path.GetFullPath(source.DataSource);
		string backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "backups");
		Directory.CreateDirectory(backupDirectory);
		string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
		string backupPath = Path.Combine(backupDirectory, $"backup_{timestamp}.db");
		for (int suffix = 1; File.Exists(backupPath); suffix++)
			backupPath = Path.Combine(backupDirectory, $"backup_{timestamp}_{suffix}.db");

		using SqliteConnection backup = new(new SqliteConnectionStringBuilder
		{
			DataSource = backupPath,
			Pooling = false
		}.ToString());
		backup.Open();
		source.BackupDatabase(backup);
		return backupPath;
	}
}
