using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Quartz;
using SchulCloud.Authorization;
using SchulCloud.DbManager.Options;
using SchulCloud.ServiceDefaults.Services;
using SchulCloud.Store.Managers;
using System.Diagnostics;
using System.Reflection;

namespace SchulCloud.DbManager.Initialization;

internal class InitializerJob(ILogger<InitializerJob> logger, IServiceProvider _services, IOptionsMonitor<DefaultUserOptions> defaultUserOptions) : IJob
{
    private DefaultUserOptions DefaultUserOption => defaultUserOptions.CurrentValue;

    public const string ActivitySourceName = nameof(InitializerJob);

    public async Task Execute(IJobExecutionContext context)
    {
        using IServiceScope serviceScope = _services.CreateScope();
        IDataManager manager = serviceScope.ServiceProvider.GetRequiredService<IDataManager>();
        AppUserManager<ApplicationUser> userManager = serviceScope.ServiceProvider.GetRequiredService<AppUserManager<ApplicationUser>>();

        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            await manager.InitializeDataSourceAsync(context.CancellationToken).ConfigureAwait(false);

            ApplicationUser? existingUser = await userManager.FindByNameAsync(DefaultUserOption.UserName).ConfigureAwait(false);
            existingUser ??= await userManager.FindByEmailAsync(DefaultUserOption.Email).ConfigureAwait(false);
            if (existingUser is null)
            {
                ApplicationUser user = new();
                await userManager.SetUserNameAsync(user, DefaultUserOption.UserName).ConfigureAwait(false);
                await userManager.SetEmailAsync(user, DefaultUserOption.Email).ConfigureAwait(false);

                IdentityResult result = await userManager.CreateAsync(user, DefaultUserOption.Password);
                if (result.Succeeded)
                {
                    string userId = await userManager.GetUserIdAsync(user).ConfigureAwait(false);
                    logger.LogInformation("Created new admin user '{userName}' ('{userId}').", DefaultUserOption.UserName, userId);
                }
                else
                {
                    logger.LogError("An error occurred while creating default admin user. Errors: {@errors}", result.Errors);
                    return;
                }

                // Elevate the admin's permissions as high as possible
                IEnumerable<string> permissions = typeof(Permissions)
                    .GetFields(BindingFlags.Static | BindingFlags.Public)
                    .Select(field => (string)field.GetValue(null)!);
                foreach (string permission in permissions)
                {
                    await userManager.SetPermissionLevelAsync(user, new(permission, PermissionLevel.Special)).ConfigureAwait(false);
                }
            }
            else
            {
                string? userName = await userManager.GetUserNameAsync(existingUser).ConfigureAwait(false);     // This also can be executed when the username doesn't but the email matches them of the default user.
                string userId = await userManager.GetUserIdAsync(existingUser).ConfigureAwait(false);
                logger.LogInformation("Default admin user '{userName}' ('{userId}') already exists.", userName, userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Initialization failed due an unexpected error.");
        }
        finally
        {
            sw.Stop();
        }


        logger.LogInformation("Database initialization completed successful after {time}ms", sw.ElapsedMilliseconds);
    }
}
