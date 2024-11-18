using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SchulCloud.Store.Abstractions;
using System.Drawing;

namespace SchulCloud.Store.Managers;

public class SchulCloudRoleManager<TRole>(
    IRoleStore<TRole> store,
    IEnumerable<IRoleValidator<TRole>> roleValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    ILogger<RoleManager<TRole>> logger)
    : RoleManager<TRole>(store, roleValidators, keyNormalizer, errors, logger)
    where TRole : class
{
    public override async Task<IdentityResult> DeleteAsync(TRole role)
    {
        IRoleDefaultRoleStore<TRole> store = GetDefaultRoleStore();
        bool defaultRole = await store.GetIsDefaultRoleAsync(role, CancellationToken).ConfigureAwait(false);

        if (defaultRole)
        {
            IdentityError error = new()
            {
                Code = "RoleNotDeletable",
                Description = "The role can't be deleted because it is a default role."
            };
            return IdentityResult.Failed(error);
        }

        return await base.DeleteAsync(role).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the flag that indicates whether this is a default role.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns>The flag.</returns>
    public async Task<bool> GetIsDefaultRoleAsync(TRole role)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        IRoleDefaultRoleStore<TRole> store = GetDefaultRoleStore();

        return await store.GetIsDefaultRoleAsync(role, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the flag that indicates whether this is a default role.
    /// </summary>
    /// <param name="role">The role to modify.</param>
    /// <param name="isDefault">The flag to set.</param>
    /// <returns>The result.</returns>
    public async Task<IdentityResult> SetIsDefaultRoleAsync(TRole role, bool isDefault)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        IRoleDefaultRoleStore<TRole> store = GetDefaultRoleStore();

        await store.SetIsDefaultRoleAsync(role, isDefault, CancellationToken).ConfigureAwait(false);
        return await UpdateAsync(role).ConfigureAwait(false);
    }

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

    private IRoleDefaultRoleStore<TRole> GetDefaultRoleStore()
    {
        if (Store is not IRoleDefaultRoleStore<TRole> cast)
        {
            throw new NotSupportedException($"{nameof(IRoleDefaultRoleStore<TRole>)} isn't supported by the store.");
        }
        return cast;
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
