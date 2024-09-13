namespace SchulCloud.Store.Abstractions;

/// <summary>
/// An interface that provides a flag to determine whether passkeys sign ins is enabled for a user.
/// </summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public interface IUserPasskeysStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Gets a flag that indicates whether passkey sign in is enabled for a user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The flag.</returns>
    public Task<bool> GetPasskeysEnabledAsync(TUser user, CancellationToken ct);

    /// <summary>
    /// Sets the flag that indicates whether passkey sign ins are enabled for a user.
    /// </summary>
    /// <param name="user">The user to modify.</param>
    /// <param name="enabled">The new flag.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetPasskeysEnabledAsync(TUser user, bool enabled, CancellationToken ct);

    /// <summary>
    /// Gets the count of registered passkeys of a user.
    /// </summary>
    /// <param name="user">The user to get the count from.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The count of registered keys.</returns>
    public Task<int> GetPasskeyCountAsync(TUser user, CancellationToken ct);
}
