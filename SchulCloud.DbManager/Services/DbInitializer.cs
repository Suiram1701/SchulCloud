using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using SchulCloud.Database;
using SchulCloud.DbManager.Options;
using SchulCloud.Identity.Managers;
using System.Diagnostics;
using System.Drawing;

namespace SchulCloud.DbManager.Services;

internal class DbInitializer(IServiceProvider services, ILogger<DbInitializer> logger) : BackgroundService
{
    private readonly IServiceProvider _services = services;
    private readonly ILogger _logger = logger;

    public const string ActivitySourceName = nameof(DbInitializer);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope serviceScope = _services.CreateScope();
        using Activity? activity = new ActivitySource(ActivitySourceName).StartActivity("Initialize Database", ActivityKind.Client);
        Stopwatch sw = Stopwatch.StartNew();

        await MigrateDbAsync(serviceScope.ServiceProvider, activity, stoppingToken).ConfigureAwait(false);
        await AddDefaultRolesAsync(serviceScope.ServiceProvider, activity, stoppingToken).ConfigureAwait(false);
        await AddDefaultUserAsync(serviceScope.ServiceProvider, activity).ConfigureAwait(false);

        _logger.LogInformation("Database initialization completed successful after {time}ms", sw.ElapsedMilliseconds);
    }

    private async Task MigrateDbAsync(IServiceProvider serviceProvider, Activity? activity, CancellationToken ct)
    {
        DbContext context = serviceProvider.GetRequiredService<AppDbContext>();
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        IEnumerable<string> pendingMigrations = await strategy.ExecuteAsync(context.Database.GetPendingMigrationsAsync, ct);
        if (pendingMigrations.Any())
        {
            _logger.LogInformation("Migrations {migrations} aren't applied to the database yet.", string.Join(", ", pendingMigrations));

            try
            {
                await strategy.ExecuteAsync(context.Database.MigrateAsync, ct);

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
    }

    private async Task AddDefaultRolesAsync(IServiceProvider serviceProvider, Activity? activity, CancellationToken ct)
    {
        AppRoleManager<ApplicationRole> roleManager = serviceProvider.GetRequiredService<AppRoleManager<ApplicationRole>>();
        IEnumerable<string?> defaultRoles = await roleManager.Roles
            .Where(role => role.DefaultRole)
            .Select(role => role.Name)
            .ToListAsync(ct);

        await TryAddDefaultRoleAsync(roleManager, RoleNames.AdminRoleName, Color.Red, defaultRoles, activity);
        await TryAddDefaultRoleAsync(roleManager, RoleNames.TeacherRoleName, Color.Blue, defaultRoles, activity);
        await TryAddDefaultRoleAsync(roleManager, RoleNames.StudentRoleName, Color.Green, defaultRoles, activity);
    }

    private async Task TryAddDefaultRoleAsync(AppRoleManager<ApplicationRole> manager, string roleName, Color? roleColor, IEnumerable<string?> roles, Activity? activity)
    {
        if (!roles.Contains(roleName))
        {
            ApplicationRole role = new();
            await manager.SetRoleNameAsync(role, roleName).ConfigureAwait(false);
            await manager.SetRoleColorAsync(role, roleColor).ConfigureAwait(false);
            await manager.SetIsDefaultRoleAsync(role, true).ConfigureAwait(false);

            IdentityResult result = await manager.CreateAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Created default role '{role}' with id {id}.", roleName, role.Id);
                activity?.AddEvent(new($"Added role '{roleName}'."));
            }
            else
            {
                activity?.AddEvent(new($"Unable to create role '{roleName}'."));

                string errorString = string.Join('\n', result.Errors.Select(error => $"Code: {error.Code}, Description: {error.Description}"));
                _logger.LogError("Unable to create default role '{role}'. Errors:\n{errors}", roleName, errorString);
            }
        }
    }

    private async Task AddDefaultUserAsync(IServiceProvider serviceProvider, Activity? activity)
    {
        UserManager<ApplicationUser> manager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        IEnumerable<ApplicationUser> adminUsers = await manager.GetUsersInRoleAsync(RoleNames.AdminRoleName);
        if (!adminUsers.Any())
        {
            DefaultUserOptions userOptions = serviceProvider.GetRequiredService<IOptions<DefaultUserOptions>>().Value;

            ApplicationUser user = new();
            await manager.SetUserNameAsync(user, userOptions.UserName).ConfigureAwait(false);
            await manager.SetEmailAsync(user, userOptions.Email).ConfigureAwait(false);

            IdentityResult result = await manager.CreateAsync(user, userOptions.Password);

            if (result.Succeeded)
            {
                result = await manager.AddToRoleAsync(user, RoleNames.AdminRoleName);

                string userId = await manager.GetUserIdAsync(user).ConfigureAwait(false);

                _logger.LogInformation("Created admin user with id {id}.", userId);
                activity?.AddEvent(new("Admin user created"));
            }
            else
            {
                activity?.AddEvent(new($"Unable to create default admin user."));

                string errorString = string.Join('\n', result.Errors.Select(error => $"Code: {error.Code}, Description: {error.Description}"));
                _logger.LogError("Unable to create default admin user. Errors:\n{errors}", errorString);
            }
        }
    }
}
