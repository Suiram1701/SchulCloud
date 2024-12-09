using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using SchulCloud.Authorization;
using SchulCloud.Database;
using SchulCloud.DbManager.Options;
using SchulCloud.Store.Managers;
using System.Diagnostics;
using System.Reflection;

namespace SchulCloud.DbManager.Initialization;

internal class DbInitializer(ILogger<DbInitializer> logger, IServiceProvider _services) : BackgroundService
{
    private readonly ILogger _logger = logger;

    public const string ActivitySourceName = nameof(DbInitializer);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using Activity? activity = new ActivitySource(ActivitySourceName).StartActivity("Initialize Database", ActivityKind.Internal);
        using IServiceScope serviceScope = _services.CreateScope();
        Stopwatch sw = Stopwatch.StartNew();

        AppDbContext identityContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

        IExecutionStrategy strategy = identityContext.Database.CreateExecutionStrategy();
        IEnumerable<string> pendingMigrations = await strategy.ExecuteAsync(identityContext.Database.GetPendingMigrationsAsync, stoppingToken);
        if (pendingMigrations.Any())
        {
            _logger.LogInformation("Migrations {@migrations} aren't applied to the database yet.", pendingMigrations);

            try
            {
                await strategy.ExecuteAsync(identityContext.Database.MigrateAsync, stoppingToken);

                _logger.LogInformation("Missing migrations applied successful.");
                activity?.AddEvent(new($"Migrations {string.Join(", ", pendingMigrations)} applied successful to the database."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while applying missing database migrations.");
                activity?.AddEvent(new("An error occurred while applying missing database migrations."));

                throw;
            }
        }
        else
        {
            _logger.LogInformation("Database migrations are up to date.");
        }

        await AddDefaultUserAsync(serviceScope.ServiceProvider, activity).ConfigureAwait(false);

        sw.Stop();
        _logger.LogInformation("Database initialization completed successful after {time}ms", sw.ElapsedMilliseconds);
    }

    private async Task AddDefaultUserAsync(IServiceProvider serviceProvider, Activity? activity)
    {
        AppUserManager<ApplicationUser> manager = serviceProvider.GetRequiredService<AppUserManager<ApplicationUser>>();
        DefaultUserOptions userOptions = serviceProvider.GetRequiredService<IOptions<DefaultUserOptions>>().Value;

        ApplicationUser user = new();
        await manager.SetUserNameAsync(user, userOptions.UserName).ConfigureAwait(false);
        await manager.SetEmailAsync(user, userOptions.Email).ConfigureAwait(false);

        IdentityResult result = await manager.CreateAsync(user, userOptions.Password);
        if (result.Succeeded)
        {
            string userId = await manager.GetUserIdAsync(user).ConfigureAwait(false);

            _logger.LogInformation("Created admin user with id {id}.", userId);
            activity?.AddEvent(new("Admin user created"));
        }
        else
        {
            activity?.AddEvent(new($"Unable to create default admin user."));

            string errorString = string.Join('\n', result.Errors.Select(error => $"Code: {error.Code}, Description: {error.Description}"));
            _logger.LogError("Unable to create default admin user. Errors:\n{errors}", errorString);
            return;
        }

        IEnumerable<string> permissions = typeof(Permissions)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(field => (string)field.GetValue(null)!);
        foreach (string permission in permissions)
        {
            await manager.SetPermissionLevelAsync(user, new(permission, PermissionLevel.Special)).ConfigureAwait(false);
        }
    }
}
