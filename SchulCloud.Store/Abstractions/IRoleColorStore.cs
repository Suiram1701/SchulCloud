using System.Drawing;

namespace SchulCloud.Store.Abstractions;

/// <summary>
/// A store interface that provides get and set of the role.
/// </summary>
/// <typeparam name="TRole">The type of the role.</typeparam>
public interface IRoleColorStore<TRole>
    where TRole : class
{
    /// <summary>
    /// Gets the role color of the specified role.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The color of the role.</returns>
    public Task<Color?> GetRoleColorAsync(TRole role, CancellationToken ct = default);

    /// <summary>
    /// Sets the role color of the specified role.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="color">The color to set.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetRoleColorAsync(TRole role, Color? color, CancellationToken ct = default);
}
