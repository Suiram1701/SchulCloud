using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using SchulCloud.Database;
using SchulCloud.Database.Models;
using SchulCloud.DbManager.Options;
using System.Diagnostics;

namespace SchulCloud.DbManager;

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
        SchulCloudDbContext context = serviceProvider.GetRequiredService<SchulCloudDbContext>();

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
        RoleManager<Role> roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();

        IEnumerable<string?> defaultRoles = await roleManager.Roles
            .Where(role => role.DefaultRole)
            .Select(role => role.Name)
            .ToListAsync(ct);

        await TryAddDefaultRoleAsync(RoleNames.AdminRoleName, "#FF0000", defaultRoles, roleManager, activity);
        await TryAddDefaultRoleAsync(RoleNames.TeacherRoleName, "#0000FF", defaultRoles, roleManager, activity);
        await TryAddDefaultRoleAsync(RoleNames.StudentRoleName, "#008000", defaultRoles, roleManager, activity);
    }

    private async Task TryAddDefaultRoleAsync(string roleName, string hexColor, IEnumerable<string?> roles, RoleManager<Role> manager, Activity? activity)
    {
        if (!roles.Contains(roleName))
        {
            Role role = new()
            {
                Name = roleName,
                Color = hexColor,
                DefaultRole = true
            };
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
        UserManager<User> userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        IEnumerable<User> adminUsers = await userManager.GetUsersInRoleAsync(RoleNames.AdminRoleName);
        if (!adminUsers.Any())
        {
            DefaultUserOptions userOptions = serviceProvider.GetRequiredService<IOptions<DefaultUserOptions>>().Value;

            User user = new()
            {
                UserName = userOptions.UserName,
                Email = userOptions.Email,
                EmailConfirmed = true
            };
            IdentityResult result = await userManager.CreateAsync(user, userOptions.Password);

            if (result.Succeeded)
            {
                result = await userManager.AddToRoleAsync(user, RoleNames.AdminRoleName);

                string userId = await userManager.GetUserIdAsync(user).ConfigureAwait(false);

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
