using Microsoft.AspNetCore.Identity;
using Quartz;
using SchulCloud.Frontend.Services.Interfaces;
using SchulCloud.Frontend.Services.Models;
using SchulCloud.Identity.Models;
using System.Diagnostics;

namespace SchulCloud.Frontend.Jobs;

public class LoginAttemptProcessJob(IServiceScopeFactory scopeFactory, ILogger<LoginAttemptProcessJob> logger, IIPGeolocator geolocator) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ActivityContext? triggerActivity = context.MergedJobDataMap.Get("trigger") as ActivityContext?;
        if (triggerActivity is not null)
        {
            Activity.Current?.AddLink(new(triggerActivity.Value));
        }

        if (context.MergedJobDataMap.Get("user") is not ApplicationUser user
            || context.MergedJobDataMap.Get("attempt") is not UserLoginAttempt attempt)
        {
            throw new JobExecutionException(
                cause: new InvalidOperationException($"Unable to run job '{context.JobDetail.Key}' without job data 'user' and 'attempt'."),
                refireImmediately: false);
        }

        using IServiceScope serviceScope = scopeFactory.CreateScope();
        ApplicationUserManager userManager = serviceScope.ServiceProvider.GetRequiredService<ApplicationUserManager>();

        string userId = (await userManager.GetUserIdAsync(user))!;
        Activity.Current?.AddTag("parameters.userId", userId);

        if (attempt.IpAddress is not null)
        {
            IPGeoLookupResult? ipLookupResult = await geolocator.GetLocationAsync(attempt.IpAddress, context.CancellationToken);
            if (ipLookupResult is not null)
            {
                attempt.Longitude = ipLookupResult.Longitude;
                attempt.Latitude = ipLookupResult.Latitude;
            }
            else
            {
                logger.LogDebug("IP address lookup for a address '{address}' failed.", attempt.IpAddress);
            }
        }

        IdentityResult logResult = await userManager.AddLoginAttemptAsync(user, attempt);
        if (logResult.Succeeded)
        {
            logger.LogTrace("Logged login attempt'.");
        }
        else
        {
            logger.LogError("An error occurred while saving login attempt.");
        }
    }
}
