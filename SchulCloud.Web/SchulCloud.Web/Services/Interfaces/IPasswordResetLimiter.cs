using Microsoft.AspNetCore.Identity;
using SchulCloud.Web.Options;

namespace SchulCloud.Web.Services.Interfaces;

/// <summary>
/// An interfaces that persists a password reset timeout for user.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IPasswordResetLimiter<TUser>
    where TUser : IdentityUser
{
    /// <summary>
    /// The options of the limiter.
    /// </summary>
    public PasswordResetLimiterOptions Options { get; }

    /// <summary>
    /// Indicates whether the user is allowed to request a password reset.
    /// </summary>
    /// <remarks>
    /// When the user is allowed the timeout will automatically saved.
    /// </remarks>
    /// <param name="user">The user</param>
    /// <returns>Indicates whether the user is allowed or not.</returns>
    public bool CanRequestPasswordReset(TUser user);

    /// <summary>
    /// Get the expiration time of the timeout.
    /// </summary>
    /// <remarks>
    /// When the timed out of the user isn't set <see cref="DateTimeOffset.MinValue"/> will be returned.
    /// </remarks>
    /// <param name="user"></param>
    /// <returns></returns>
    public DateTimeOffset GetExpirationTime(TUser user);
}
