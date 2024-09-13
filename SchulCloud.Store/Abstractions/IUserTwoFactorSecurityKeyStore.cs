namespace SchulCloud.Store.Abstractions;

/// <summary>
/// An interface that provides a flags whether two factor via security keys is enabled.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IUserTwoFactorSecurityKeyStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Gets the flag whether the two factor via security keys is enabled.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The flag.</returns>
    public Task<bool> GetTwoFactorSecurityKeyEnabledAsync(TUser user, CancellationToken ct);

    /// <summary>
    /// Sets the flag whether the two factor via security keys is enabled.
    /// </summary>
    /// <param name="user">The user to modify.</param>
    /// <param name="enabled">The new flag.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetTwoFactorSecurityKeyEnabledAsync(TUser user, bool enabled, CancellationToken ct);

    /// <summary>
    /// Gets the count of registered security keys for a user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The count of keys.</returns>
    public Task<int> GetTwoFactorSecurityKeyCountAsync(TUser user, CancellationToken ct);
}