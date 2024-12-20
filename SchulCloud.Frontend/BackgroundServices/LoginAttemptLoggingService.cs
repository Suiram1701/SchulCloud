using Microsoft.AspNetCore.Identity;
using SchulCloud.Frontend.Services.Interfaces;
using SchulCloud.Frontend.Services.Models;
using SchulCloud.Identity.Models;
using System.Diagnostics;
using System.Threading.Channels;

namespace SchulCloud.Frontend.BackgroundServices;

public class LoginAttemptLoggingService(ILogger<LoginAttemptLoggingService> logger, IServiceScopeFactory scopeFactory, IIPGeolocator geolocator) : BackgroundService
{
    private readonly Channel<QueueredAttempt> _attemptsChannel = Channel.CreateUnbounded<QueueredAttempt>(new() { SingleReader = true });

    public const string ActivitySourceName = nameof(LoginAttemptLoggingService);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using ActivitySource activitySource = new(ActivitySourceName);
        await foreach (QueueredAttempt queueredAttempt in _attemptsChannel.Reader.ReadAllAsync(stoppingToken))
        {
            queueredAttempt.Deconstruct(out ApplicationUser user, out UserLoginAttempt attempt, out ActivityContext? triggerContext);

            using Activity? activity = activitySource.StartActivity("process login attempt");
            if (triggerContext is not null)
            {
                activity?.AddLink(new(triggerContext.Value));
            }

            using IServiceScope serviceScope = scopeFactory.CreateScope();
            ApplicationUserManager userManager = serviceScope.ServiceProvider.GetRequiredService<ApplicationUserManager>();

            try
            {
                string userId = (await userManager.GetUserIdAsync(user))!;

                if (attempt.IpAddress is not null)
                {
                    IPGeoLookupResult? ipLookupResult = await geolocator.GetLocationAsync(attempt.IpAddress, stoppingToken);
                    if (ipLookupResult is not null)
                    {
                        attempt.Longitude = ipLookupResult.Longitude;
                        attempt.Latitude = ipLookupResult.Latitude;
                    }
                    else
                    {
                        logger.LogDebug("IP address lookup for a login attempt for user '{userId}' failed.", userId);
                    }
                }

                IdentityResult logResult = await userManager.AddLoginAttemptAsync(user, attempt);
                if (logResult.Succeeded)
                {
                    logger.LogTrace("Logged login attempt for user '{userId}'.", userId);
                }
                else
                {
                    logger.LogError("An error occurred while saving an login attempt for user '{userId}'.", userId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occurred while logging a login attempt.");
            }
        }
    }

    /// <summary>
    /// Enqueues an login attempt to locate its position and write it to the db.
    /// </summary>
    /// <param name="user">The user the attempt is associated with.</param>
    /// <param name="attempt">The attempt to enqueue.</param>
    public async Task EnqueueAttemptAsync(ApplicationUser user, UserLoginAttempt attempt)
    {
        await _attemptsChannel.Writer.WriteAsync(new(user, attempt, Activity.Current?.Context));
    }

    private record QueueredAttempt(ApplicationUser User, UserLoginAttempt Attempt, ActivityContext? TriggerContext);
}
