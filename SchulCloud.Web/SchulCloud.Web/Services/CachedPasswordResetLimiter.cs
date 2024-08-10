using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SchulCloud.Web.Options;
using SchulCloud.Web.Services.Interfaces;

namespace SchulCloud.Web.Services;

/// <summary>
/// A implementation of <see cref="IPasswordResetLimiter{TUser}"/> that uses <see cref="IMemoryCache"/> to persists the timeouts.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
/// <param name="cache">The cache to use.</param>
/// <param name="optionsAccessor">The options accessor.</param>
public class CachedPasswordResetLimiter<TUser>(IMemoryCache cache, IOptions<PasswordResetOptions> optionsAccessor) : IPasswordResetLimiter<TUser>
    where TUser : IdentityUser
{
    private readonly IMemoryCache _cache = cache;
    private readonly IOptions<PasswordResetOptions> _optionsAccessor = optionsAccessor;

    public PasswordResetOptions Options => _optionsAccessor.Value;

    public bool CanRequestPasswordReset(TUser user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        string cacheKey = GetCacheKey(user);
        if (_cache.TryGetValue(cacheKey, out _))
        {
            return false;
        }
        else
        {
            DateTimeOffset expiration = DateTimeOffset.UtcNow.Add(Options.ResetTimeout);
            _cache.Set(cacheKey, expiration, expiration);

            return true;
        }
    }

    public DateTimeOffset GetExpirationTime(TUser user)
    {
        string cacheKey = GetCacheKey(user);
        if (_cache.TryGetValue(cacheKey, out DateTimeOffset expiration))
        {
            return expiration;
        }
        else
        {
            return DateTimeOffset.MinValue;
        }
    }

    private static string GetCacheKey(TUser user)
    {
        return $"PasswordResetRequest_{user.Id}";
    }
}
