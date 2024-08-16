using SchulCloud.Web.Services.Interfaces;

namespace SchulCloud.Web.Extensions;

public static class RequestLimiterExtensions
{
    public const string PasswordResetPurpose = "PasswordReset";

    /// <summary>
    /// Indicates whether a password reset is allowed for the user.
    /// </summary>
    /// <inheritdoc cref="IRequestLimiter{TUser}.CanRequestAsync(TUser, string)"/>
    public static async Task<bool> CanRequestPasswordResetAsync<TUser>(this IRequestLimiter<TUser> limiter, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(limiter);
        ArgumentNullException.ThrowIfNull(user);

        return await limiter.CanRequestAsync(user, PasswordResetPurpose);
    }

    /// <summary>
    /// Gets the expiration time of the password reset timeout of the user.
    /// </summary>
    /// <inheritdoc cref="IRequestLimiter{TUser}.GetExpirationTimeAsync(TUser, string)"/>
    public static async Task<DateTimeOffset?> GetPasswordResetExpirationTimeAsync<TUser>(this IRequestLimiter<TUser> limiter, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(limiter, nameof(limiter));
        ArgumentNullException.ThrowIfNull(user);

        return await limiter.GetExpirationTimeAsync(user, PasswordResetPurpose);
    }

    public const string TwoFactorEmailPurpose = "TwoFactorEmail";

    /// <summary>
    /// Indicates whether the user is currently allowed to send a 2fa email code.
    /// </summary>
    /// <inheritdoc cref="IRequestLimiter{TUser}.CanRequestAsync(TUser, string)"/>
    public static async Task<bool> CanRequestTwoFactorEmailCodeAsync<TUser>(this IRequestLimiter<TUser> limiter, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(limiter);
        ArgumentNullException.ThrowIfNull(user);

        return await limiter.CanRequestAsync(user, TwoFactorEmailPurpose);
    }

    /// <summary>
    /// Gets the expiration time of the 2fa email code timeout of the user.
    /// </summary>
    /// <inheritdoc cref="IRequestLimiter{TUser}.GetExpirationTimeAsync(TUser, string)"/>
    public static async Task<DateTimeOffset?> GetTwoFactorEmailCodeExpirationTimeAsync<TUser>(this IRequestLimiter<TUser> limiter, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(limiter, nameof(limiter));
        ArgumentNullException.ThrowIfNull(user);

        return await limiter.GetExpirationTimeAsync(user, TwoFactorEmailPurpose);
    }
}
