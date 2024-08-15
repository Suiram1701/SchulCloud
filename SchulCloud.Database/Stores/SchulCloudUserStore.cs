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

public class SchulCloudUserStore(DbContext context, IdentityErrorDescriber? describer = null)
    : UserStore<User, Role, DbContext>(context, describer)
{
    public override Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken cancellationToken = default)
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
