namespace SchulCloud.Database.Stores;

/// <summary>
/// An interface that provides a flag that indicates whether 2fa via email is enabled.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IUserTwoFactorEmailStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Gets the current flag.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The flag.</returns>
    public Task<bool> GetTwoFactorEmailEnabled(TUser user, CancellationToken ct = default);

    /// <summary>
    /// Sets the flag.
    /// </summary>
    /// <param name="user">The user to modify.</param>
    /// <param name="enabled">The flag to set.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetTwoFactorEmailEnabled(TUser user, bool enabled, CancellationToken ct = default);
}
