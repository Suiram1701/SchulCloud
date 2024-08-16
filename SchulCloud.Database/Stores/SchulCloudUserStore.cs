using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database.Enums;
using SchulCloud.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.Stores;

public class SchulCloudUserStore(SchulCloudDbContext context, IdentityErrorDescriber? describer = null)
    : UserStore<User, Role, DbContext>(context, describer)
{
    public override Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.TwoFactorEnabledFlags.HasFlag(TwoFactorMethod.Authenticator));
    }

    public override Task SetTwoFactorEnabledAsync(User user, bool enabled, CancellationToken ct = default)
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

    public Task<bool> GetTwoFactorEmailEnabled(User user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(nameof(user));

        return Task.FromResult(user.TwoFactorEnabled.HasFlag(TwoFactorEnabled.Authenticator));
    }

    public override Task SetTwoFactorEnabledAsync(User user, bool enabled, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(nameof(user));

        user.TwoFactorEnabled |= TwoFactorEnabled.Authenticator;
        return Task.CompletedTask;
    }
}
