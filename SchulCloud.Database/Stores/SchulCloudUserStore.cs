using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database.Enums;
using SchulCloud.Database.Models;
using SchulCloud.Store.Abstractions;

namespace SchulCloud.Database.Stores;

public class SchulCloudUserStore<TUser, TRole, TContext>(TContext context, IdentityErrorDescriber? describer = null)
    : UserStore<TUser, TRole, TContext>(context, describer),
    IUserTwoFactorEmailStore<TUser>
    where TUser : SchulCloudUser
    where TRole : IdentityRole
    where TContext : DbContext
{
    public override Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.TwoFactorEnabledFlags.HasFlag(TwoFactorMethod.Authenticator));
    }

    public override Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (enabled)
        {
            user.TwoFactorEnabledFlags |= TwoFactorMethod.Authenticator;
        }
        else
        {
            user.TwoFactorEnabledFlags &= ~TwoFactorMethod.Authenticator;
        }
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEmailEnabled(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.TwoFactorEnabledFlags.HasFlag(TwoFactorMethod.Email));
    }

    public Task SetTwoFactorEmailEnabled(TUser user, bool enabled, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (enabled)
        {
            user.TwoFactorEnabledFlags |= TwoFactorMethod.Email;
        }
        else
        {
            user.TwoFactorEnabledFlags &= ~TwoFactorMethod.Email;
        }
        return Task.CompletedTask;
    }
}
