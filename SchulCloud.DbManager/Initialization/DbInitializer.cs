using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using SchulCloud.Authorization;
using SchulCloud.Database;
using SchulCloud.DbManager.Options;
using SchulCloud.ServiceDefaults.Services;
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

        IDataManager manager = serviceScope.ServiceProvider.GetRequiredService<IDataManager>();
        await manager.InitializeDataSourceAsync(stoppingToken).ConfigureAwait(false);

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
        }
        else
        {
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
