﻿using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SchulCloud.Frontend.Options;
using SchulCloud.Frontend.Services.Interfaces;

namespace SchulCloud.Frontend.Services;

/// <summary>
/// A implementation of <see cref="IRequestLimiter{TUser}"/> that uses <see cref="IMemoryCache"/> to persists the timeouts.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
/// <param name="cache">The cache to use.</param>
/// <param name="optionsAccessor">The options accessor.</param>
public class CachedRequestLimiter<TUser>(IMemoryCache cache, IOptions<RequestLimiterOptions> optionsAccessor, UserManager<TUser> userManager) : IRequestLimiter<TUser>
    where TUser : class
{
    private readonly IMemoryCache _cache = cache;
    private readonly UserManager<TUser> _userManager = userManager;
    private readonly RequestLimiterOptions _options = optionsAccessor.Value;

    public RequestLimiterOptions Options => _options;

    public async Task<bool> CanRequestAsync(TUser user, string purpose)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        string cacheKey = await GetCacheKeyAsync(user, purpose);
        if (!_cache.TryGetValue(cacheKey, out _))
        {
            if (!_options.Timeouts.TryGetValue(purpose, out TimeSpan value))
            {
                value = _options.DefaultTimeout;
            }

            DateTimeOffset expiration = DateTimeOffset.UtcNow.Add(value);

            using ICacheEntry entry = _cache.CreateEntry(cacheKey)
                .SetAbsoluteExpiration(expiration)
                .SetValue(expiration)
                .SetSize(16);     // Estimated size of DateTimeOffset tried using sizeof in CSharpREPL (using sizeof here would require to enable unsafe).
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task<DateTimeOffset?> GetTimeoutAsync(TUser user, string purpose)
    {
        string cacheKey = await GetCacheKeyAsync(user, purpose);
        if (_cache.TryGetValue(cacheKey, out DateTimeOffset expiration))
        {
            return expiration;
        }
        else
        {
            return null;
        }
    }

    private async Task<string> GetCacheKeyAsync(TUser user, string purpose)
    {
        string userId = await _userManager.GetUserIdAsync(user);
        return $"requestLimit_{purpose}_{userId}";
    }
}
