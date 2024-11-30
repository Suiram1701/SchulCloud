using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database.Models;
using SchulCloud.Identity.Abstractions;
using System.Drawing;

namespace SchulCloud.Database.Stores;

public class SchulCloudRoleStore<TRole, TContext>(TContext context, IdentityErrorDescriber? describer = null)
    : RoleStore<TRole, DbContext>(context, describer),
    IRoleDefaultRoleStore<TRole>,
    IRoleColorStore<TRole>
    where TRole : AppRole
    where TContext : DbContext
{
    public Task<bool> GetIsDefaultRoleAsync(TRole role, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(role);

        return Task.FromResult(role.DefaultRole);
    }

    public Task SetIsDefaultRoleAsync(TRole role, bool isDefault, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(role);

        role.DefaultRole = isDefault;
        return Task.CompletedTask;
    }

    public Task<Color?> GetRoleColorAsync(TRole role, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(role);

        Color? color = role.ArgbColor is not null
            ? Color.FromArgb(role.ArgbColor.Value)
            : null;
        return Task.FromResult(color);
    }

    public Task SetRoleColorAsync(TRole role, Color? color, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(role);

        role.ArgbColor = color?.ToArgb();
        return Task.CompletedTask;
    }
}
