using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SchulCloud.Identity.Abstractions;
using System.Drawing;

namespace SchulCloud.Identity.Managers;

public class AppRoleManager<TRole>(
    IRoleStore<TRole> store,
    IEnumerable<IRoleValidator<TRole>> roleValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    ILogger<RoleManager<TRole>> logger)
    : RoleManager<TRole>(store, roleValidators, keyNormalizer, errors, logger)
    where TRole : class
{
    /// <summary>
    /// Gets the color of the role.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns>The color of the role.</returns>
    public async Task<Color?> GetRoleColorAsync(TRole role)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);

        IRoleColorStore<TRole> store = GetColorStore();
        return await store.GetRoleColorAsync(role, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the color of the role.
    /// </summary>
    /// <param name="role">The role to modify.</param>
    /// <param name="color">The color to set.</param>
    /// <returns>The result.</returns>
    public async Task<IdentityResult> SetRoleColorAsync(TRole role, Color? color)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        IRoleColorStore<TRole> store = GetColorStore();

        await store.SetRoleColorAsync(role, color, CancellationToken).ConfigureAwait(false);
        return await UpdateAsync(role).ConfigureAwait(false);
    }

    private IRoleColorStore<TRole> GetColorStore()
    {
        if (Store is not IRoleColorStore<TRole> cast)
        {
            throw new NotSupportedException($"{nameof(IRoleColorStore<TRole>)} isn't supported by the store.");
        }
        return cast;
    }
}
