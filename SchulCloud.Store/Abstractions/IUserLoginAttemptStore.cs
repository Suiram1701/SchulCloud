using SchulCloud.Store.Enums;
using SchulCloud.Store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Store.Abstractions;

 /// <summary>
/// A store interface that provides login attempts to a user's account.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IUserLoginAttemptStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Finds a login attempt by its id.
    /// </summary>
    /// <param name="id">The id of the attempt.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The attempt. If <c>null</c> the attempt wasn't found.</returns>
    public Task<UserLoginAttempt?> FindLoginAttemptAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by a login attempt owned by him.
    /// </summary>
    /// <param name="attempt">The attempt to find the user with.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user that owns the attempt.</returns>
    public Task<TUser?> FindUserByLoginAttemptAsync(UserLoginAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Gets all log in attempts of a user.
    /// </summary>
    /// <param name="user">The user to get the attempts for.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The attempts</returns>
    public Task<IEnumerable<UserLoginAttempt>> FindLoginAttemptsByUserAsync(TUser user, CancellationToken ct = default);

    /// <summary>
    /// Adds a log in attempt for a user.
    /// </summary>
    /// <param name="user">The user to add this attempt to.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created attempt.</returns>
    public Task AddLoginAttemptAsync(TUser user, UserLoginAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Removes a log in attempt of the user.
    /// </summary>
    /// <param name="attempt">The attempt to remove.</param>
    /// <param name="ct">Cancellation token</param>
    public Task RemoveLoginAttemptAsync(UserLoginAttempt attempt, CancellationToken ct = default);

    /// <summary>
    /// Removes all log in attempts of a user.
    /// </summary>
    /// <param name="user">The user to delete the attempts for.</param>
    /// <param name="ct">Cancellation token</param>
    public Task RemoveAllLoginAttemptsAsync(TUser user, CancellationToken ct = default);

    /// <summary>
    /// Gets the date time of the latest successful login attempt of every login method.
    /// </summary>
    /// <param name="user">The user to get the login attempts from.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>
    /// A dictionary containing pairs of the used method and the last use time.
    /// If method isn't contained method wasn't used yet.
    /// </returns>
    public Task<IReadOnlyDictionary<LoginAttemptMethod, DateTime>> GetLatestLoginMethodUseTimeAsync(TUser user, CancellationToken ct = default);
}