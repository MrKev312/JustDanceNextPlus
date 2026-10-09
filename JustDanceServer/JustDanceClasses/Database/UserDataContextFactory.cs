using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JustDanceServer.JustDanceClasses.Database;

// Migrations must not start the content loader or touch the configured player database.
public sealed class UserDataContextFactory : IDesignTimeDbContextFactory<UserDataContext>
{
	public UserDataContext CreateDbContext(string[] args)
		=> new(new DbContextOptionsBuilder<UserDataContext>()
			.UseSqlite("Data Source=:memory:").Options);
}
