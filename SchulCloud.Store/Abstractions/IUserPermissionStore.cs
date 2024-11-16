using SchulCloud.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Store.Abstractions;

/// <summary>
/// A store interface that provides permission levels for users.
/// </summary>
/// <typeparam name="TUser"></typeparam>
public interface IUserPermissionStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Sets the level of a permission to a specific level for a user.
    /// </summary>
    /// <param name="user">The user to set the permission for.</param>
    /// <param name="permissionName">The name of the permission.</param>
    /// <param name="level">The level to set.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetPermissionLevelAsync(TUser user, string permissionName, PermissionLevel level, CancellationToken ct);

    /// <summary>
    /// Gets the permission level of a permission of a user.
    /// </summary>
    /// <param name="user">The user to get this for.</param>
    /// <param name="permissionName">The name of the permission.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The level this user have for the permission.</returns>
    public Task<PermissionLevel> GetPermissionLevelAsync(TUser user, string permissionName, CancellationToken ct);

    /// <summary>
    /// Gets every permission level of a user.
    /// </summary>
    /// <param name="user">The user to get the permissions for.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A dictionary containing the name of the permission and its level.</returns>
    public Task<IReadOnlyDictionary<string, PermissionLevel>> GetPermissionLevelsAsync(TUser user, CancellationToken ct);
}
