using SchulCloud.Frontend.Services.Interfaces;

namespace SchulCloud.Frontend.Extensions;

public static class RequestLimiterExtensions
{
    public const string ConfirmEmailPurpose = "ConfirmEmail";

    /// <summary>
    /// Indicates whether a email confirmation request is allowed for a user.
    /// </summary>
    /// <inheritdoc cref="IRequestLimiter{TUser}.CanRequestAsync(TUser, string)"/>
    public static async Task<bool> CanRequestEmailConfirmationAsync<TUser>(this IRequestLimiter<TUser> limiter, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(limiter);
        ArgumentNullException.ThrowIfNull(user);

        return await limiter.CanRequestAsync(user, ConfirmEmailPurpose);
    }

    /// <summary>
    /// Gets the timeout of the email confirmation of a user.
    /// </summary>
    /// <inheritdoc cref="IRequestLimiter{TUser}.GetTimeoutAsync(TUser, string)"/>
    public static async Task<DateTimeOffset?> GetEmailConfirmationTimeoutAsync<TUser>(this IRequestLimiter<TUser> limiter, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(limiter);
        ArgumentNullException.ThrowIfNull(user);

        return await limiter.GetTimeoutAsync(user, ConfirmEmailPurpose);
    }

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
    /// Gets the timeout of the password reset of the user.
    /// </summary>
    /// <inheritdoc cref="IRequestLimiter{TUser}.GetTimeoutAsync(TUser, string)"/>
    public static async Task<DateTimeOffset?> GetPasswordResetTimeoutAsync<TUser>(this IRequestLimiter<TUser> limiter, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(limiter, nameof(limiter));
        ArgumentNullException.ThrowIfNull(user);

        return await limiter.GetTimeoutAsync(user, PasswordResetPurpose);
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
    /// Gets the timeout of the 2fa email code of the user.
    /// </summary>
    /// <inheritdoc cref="IRequestLimiter{TUser}.GetTimeoutAsync(TUser, string)"/>
    public static async Task<DateTimeOffset?> GetTwoFactorEmailCodeTimeoutAsync<TUser>(this IRequestLimiter<TUser> limiter, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(limiter, nameof(limiter));
        ArgumentNullException.ThrowIfNull(user);

        return await limiter.GetTimeoutAsync(user, TwoFactorEmailPurpose);
    }
}
