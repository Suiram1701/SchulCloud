using Microsoft.AspNetCore.Identity;
using SchulCloud.Web.Options;

namespace SchulCloud.Web.Services.Interfaces;

/// <summary>
/// An interfaces that persists a request timeout for user.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IRequestLimiter<TUser>
    where TUser : class
{
    /// <summary>
    /// The options to use.
    /// </summary>
    public RequestLimiterOptions Options { get; }

    /// <summary>
    /// Indicates whether the user is allowed to do a request with the specified purpose.
    /// </summary>
    /// <remarks>
    /// When the user is allowed the timeout will automatically saved.
    /// </remarks>
    /// <param name="user">The user</param>
    /// <param name="purpose">The purpose of the request</param>
    /// <returns>Indicates whether the user is allowed or not.</returns>
    public Task<bool> CanRequestAsync(TUser user, string purpose);

    /// <summary>
    /// Get the expiration time of the timeout.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The timeout of the user. When no timeout set <c>null</c> will be returned.</returns>
    public Task<DateTimeOffset?> GetExpirationTimeAsync(TUser user, string purpose);
}
