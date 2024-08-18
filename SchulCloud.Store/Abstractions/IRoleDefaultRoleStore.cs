namespace SchulCloud.Store.Abstractions;

/// <summary>
/// A store that provides a default flag for a role.
/// </summary>
/// <typeparam name="TRole">The type of the role.</typeparam>
public interface IRoleDefaultRoleStore<TRole>
    where TRole : class
{
    /// <summary>
    /// Gets the flag that indicates whether the role is a default role.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The flag.</returns>
    public Task<bool> GetIsDefaultRoleAsync(TRole role, CancellationToken ct = default);

    /// <summary>
    /// Sets the flag that indicates whether the role is a default role.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="isDefault">The flag value to set.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetIsDefaultRoleAsync(TRole role, bool isDefault, CancellationToken ct = default);
}
