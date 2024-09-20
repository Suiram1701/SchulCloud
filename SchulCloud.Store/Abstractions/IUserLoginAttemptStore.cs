using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Store.Abstractions;

/// <summary>
/// A store interface that provides log in attempts to a user's account.
/// </summary>
/// <typeparam name="TLogInAttempt">The type of the log in attempt.</typeparam>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IUserLoginAttemptStore<TLogInAttempt, TUser>
    where TLogInAttempt : class
    where TUser : class
{
    /// <summary>
    /// Gets all log in attempts of a user.
    /// </summary>
    /// <param name="user">The user to get the attempts for.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The attempts</returns>
    public Task<IEnumerable<TLogInAttempt>> GetLogInAttemptsOfUserAsync(TUser user, CancellationToken ct = default);

    /// <summary>
    /// Adds a log in attempt for a user.
    /// </summary>
    /// <param name="user">The user to add this attempt to.</param>
    /// <param name="methodCode">A three letter code that represents the used log in method.</param>
    /// <param name="succeeded">Indicates whether the log in attempt succeeded.</param>
    /// <param name="ipAddress">The ip address of the client who's done the attempt.</param>
    /// <param name="userAgent">The user agent used by the client who's done the attempt.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created attempt.</returns>
    public Task<TLogInAttempt> AddUserLogInAttemptAsync(TUser user, string methodCode, bool succeeded, IPAddress ipAddress, string? userAgent, CancellationToken ct = default);

    /// <summary>
    /// Removes a log in attempt of the user.
    /// </summary>
    /// <param name="attempt">The attempt to remove.</param>
    /// <param name="ct">Cancellation token</param>
    public Task RemoveUserLogInAttemptAsync(TLogInAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Removes all log in attempts of a user.
    /// </summary>
    /// <param name="user">The user to delete the attempts for.</param>
    /// <param name="ct">Cancellation token</param>
    public Task RemoveAllUserLogInAttemptsAsync(TUser user, CancellationToken ct = default);

    /// <summary>
    /// Gets the user for that a log in attempt was done.
    /// </summary>
    /// <param name="attempt">The attempt</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user</returns>
    public Task<TUser> GetLogInAttemptUserAsync(TLogInAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Gets the code of the log in method that were used for the attempt.
    /// </summary>
    /// <param name="attempt">The attempt</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The code</returns>
    public Task<string> GetLogInAttemptMethodCodeAsync(TLogInAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Gets a flag indicating whether a log in attempt succeeded.
    /// </summary>
    /// <param name="attempt">The attempt to check.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The flag</returns>
    public Task<bool> GetLogInAttemptSucceededAsync(TLogInAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Gets the ip address of the client done a log in attempt.
    /// </summary>
    /// <param name="attempt">The attempt</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The ip v4 address</returns>
    public Task<IPAddress> GetLogInAttemptIPAddressAsync(TLogInAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Gets the user agent the client used for the log in attempt.
    /// </summary>
    /// <param name="attempt">The attempt.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user agent used.</returns>
    public Task<string?> GetLogInAttemptUserAgentAsync(TLogInAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Gets the date time where a log in attempt occurred.
    /// </summary>
    /// <param name="attempt">The attempt</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The local date time.</returns>
    public Task<DateTime> GetLogInAttemptDateTimeAsync(TLogInAttempt attempt, CancellationToken ct = default);
}