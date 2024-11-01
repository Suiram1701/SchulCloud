using Microsoft.AspNetCore.Identity;
using SchulCloud.Store.Models;
using SchulCloud.Web.Services.Interfaces;
using SchulCloud.Web.Services.Models;
using System.Threading.Channels;

namespace SchulCloud.Web.Services;

public class LoginLogBackgroundService(ILogger<LoginLogBackgroundService> logger, IServiceScopeFactory scopeFactory, IIPGeolocator geolocator) : BackgroundService
{
    private readonly Channel<(ApplicationUser, UserLoginAttempt)> _channel = Channel.CreateUnbounded<(ApplicationUser, UserLoginAttempt)>(new() { SingleReader = true });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach ((ApplicationUser user, UserLoginAttempt attempt) in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using IServiceScope serviceScope = scopeFactory.CreateScope();
                AppUserManager userManager = serviceScope.ServiceProvider.GetRequiredService<AppUserManager>();

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

                IdentityResult logResult =  await userManager.AddLoginAttemptAsync(user, attempt);
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
    /// <returns></returns>
    public async Task EnqueueAttemptAsync(ApplicationUser user, UserLoginAttempt attempt)
    {
        await _channel.Writer.WriteAsync((user, attempt));
    }
}
