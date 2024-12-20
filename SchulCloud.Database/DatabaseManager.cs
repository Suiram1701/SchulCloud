using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using SchulCloud.Database.Models;
using SchulCloud.ServiceDefaults.Services;

namespace SchulCloud.Database;

/// <summary>
/// An implementation of <see cref="IDataManager"/> that manages <see cref="AppDbContext"/>.
/// </summary>
/// <param name="logger">The logger to use.</param>
/// <param name="dbContext">The Context of the database to use.</param>
public class DatabaseManager(ILogger<DatabaseManager> logger, AppDbContext dbContext) : IDataManager
{
    private DatabaseFacade Database => dbContext.Database;

    public async Task InitializeDataSourceAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Start applying pending database migrations...");

        IEnumerable<string> appliedMigrations = await Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false);
        IEnumerable<string> pendingMigrations = await Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Applied migrations: {@applied}; Pending migrations: {@pending}", appliedMigrations, pendingMigrations);

        try
        {
            await Database.MigrateAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Missing migrations successfully applied.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying pending migrations.");
            throw;
        }
    }

    public async Task RemoveDataSourceAsync(CancellationToken ct = default)
    {
        if (await Database.EnsureDeletedAsync(ct).ConfigureAwait(false))
        {
            logger.LogInformation("Database successfully removed.");
        }
        else
        {
            logger.LogInformation("Database doesn't exists.");
        }
    }

    public async Task<int> RemoveObsoleteAPIKeysAsync(CancellationToken ct = default)
    {
        int count = await dbContext.Set<ApiKey>()
            .IgnoreQueryFilters()
            .Where(key => key.Expiration <= DateTime.UtcNow)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        logger.LogDebug("Removed {count} obsolete API keys.", count);

        return count;
    }

    public async Task<int> RemoveOldLoginAttemptsAsync(int maxAttempts = -1, CancellationToken ct = default)
    {
        int count = 0;
        if (maxAttempts > -1)
        {
            count = await dbContext.Set<LoginAttempt>()
                .Where(la => dbContext.Set<LoginAttempt>()
                    .Where(inner => inner.UserId == la.UserId)
                    .OrderBy(inner => inner.DateTime)
                    .Skip(maxAttempts)
                    .Select(inner => inner.Id)
                    .Contains(la.Id))
                .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        }
        logger.LogDebug("Removed {count} login attempts.", count);

        return count;
    }
}
