using Fido2NetLib;
using Fido2NetLib.Objects;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database.Enums;
using SchulCloud.Database.Models;
using SchulCloud.Store.Abstractions;
using SchulCloud.Store.Models;
using System.Net;

namespace SchulCloud.Database.Stores;

public class SchulCloudUserStore<TUser, TRole, TContext>(TContext context, IdentityErrorDescriber? describer = null)
    : UserStore<TUser, TRole, TContext>(context, describer),
    IUserCredentialStore<TUser>,
    IUserTwoFactorEmailStore<TUser>,
    IUserTwoFactorSecurityKeyStore<TUser>,
    IUserPasskeysStore<TUser>,
    IUserLoginAttemptStore<TUser>
    where TUser : SchulCloudUser
    where TRole : IdentityRole
    where TContext : DbContext
{
    private DbSet<Credential> Credentials => Context.Set<Credential>();

    private DbSet<LoginAttempt> LoginAttempts => Context.Set<LoginAttempt>();

    private static readonly TypeAdapterConfig _loginAttemptAdaptConfig;

    static SchulCloudUserStore()
    {
        _loginAttemptAdaptConfig = new();
        _loginAttemptAdaptConfig.ForDestinationType<LoginAttempt>().Ignore(attempt => attempt.Id);
        _loginAttemptAdaptConfig.ForType<IPAddress, byte[]>().MapWith(ip => ip.GetAddressBytes());
        _loginAttemptAdaptConfig.ForType<byte[], IPAddress>().MapWith(ip => new IPAddress(ip));
    }

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

    #region IUserTwoFactorEmailStore
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
    #endregion

    #region IUserTwoFactorSecurityKeyStore
    public Task<bool> GetTwoFactorSecurityKeyEnabledAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.TwoFactorEnabledFlags.HasFlag(TwoFactorMethod.SecurityKey));
    }

    public Task SetTwoFactorSecurityKeyEnabledAsync(TUser user, bool enabled, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (enabled)
        {
            user.TwoFactorEnabledFlags |= TwoFactorMethod.SecurityKey;
        }
        else
        {
            user.TwoFactorEnabledFlags &= ~TwoFactorMethod.SecurityKey;
        }
        return Task.CompletedTask;
    }

    public async Task<int> GetTwoFactorSecurityKeyCountAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        string userId = await GetUserIdAsync(user, ct).ConfigureAwait(false);
        return await Credentials.CountAsync(cred => cred.UserId.Equals(userId), ct).ConfigureAwait(false);
    }
    #endregion

    #region IUserCredentialStore
    public async Task<UserCredential?> FindCredentialAsync(byte[] id, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(id);
        ct.ThrowIfCancellationRequested();

        Credential? credential = await Credentials.FindAsync([id], ct).AsTask();
        return credential?.Adapt<UserCredential>();
    }

    public async Task<TUser?> FindUserByCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        Credential? dbCredential = await Credentials.FindAsync([credential.Id], ct).AsTask();
        if (dbCredential is not null)
        {
            return await FindUserAsync(dbCredential.UserId, ct);
        }

        return null;
    }

    public async Task<IEnumerable<UserCredential>> FindCredentialsByUserAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        return await Credentials
            .Where(cred => cred.UserId.Equals(userId))
            .ProjectToType<UserCredential>()
            .ToListAsync(ct);
    }

    public async Task AddCredentialAsync(TUser user, UserCredential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        Credential dbDto = credential.Adapt<Credential>();
        dbDto.UserId = await GetUserIdAsync(user, ct);
        await Credentials.AddAsync(dbDto, ct).AsTask();
    }

    public async Task UpdateCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        Credential? dbDto = await Credentials.FindAsync([credential.Id], ct).AsTask();
        if (dbDto is not null)
        {
            credential.BuildAdapter()
                .EntityFromContext(Context)
                .AdaptTo(dbDto);
        }
        else
        {
            throw new InvalidOperationException($"Unable to find a credential dto with id '{credential.Id}'");
        }
    }

    public async Task RemoveCredentialAsync(UserCredential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        await Credentials.Where(cred => cred.Id.Equals(credential.Id)).ExecuteDeleteAsync(ct);
    }

    public async Task<bool> IsCredentialOwnedByUser(TUser user, UserCredential credential, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        return await Credentials.AnyAsync(cred => cred.UserId.Equals(userId) || cred.Id.SequenceEqual(credential.Id), ct);
    }
    #endregion

    #region IUserPasskeysEnabledStore
    public Task<bool> GetPasskeysEnabledAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.PasskeysEnabled);
    }

    public Task SetPasskeysEnabledAsync(TUser user, bool enabled, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        user.PasskeysEnabled = enabled;
        return Task.CompletedTask;
    }

    public async Task<bool> GetIsPasskeyCredentialAsync(UserCredential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        Credential dbCredential = await Credentials.FindAsync([credential.Id], ct).AsTask()
            ?? throw new InvalidOperationException($"Could not find entity with the id {string.Join('-', credential.Id)}.");
        return dbCredential.IsPasskey;
    }

    public async Task SetIsPasskeyCredentialAsync(UserCredential credential, bool enabled, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        Credential dbCredential = await Credentials.FindAsync([credential.Id], ct).AsTask()
            ?? throw new InvalidOperationException($"Could not find entity with the id {string.Join('-', credential.Id)}.");
        dbCredential.IsPasskey = enabled;
    }

    public async Task<int> GetPasskeyCountAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        string userId = await GetUserIdAsync(user, ct).ConfigureAwait(false);
        return await Credentials.CountAsync(cred => cred.UserId.Equals(userId) || cred.IsPasskey, ct).ConfigureAwait(false);
    }
    #endregion

    #region IUserLoginAttemptStore
    public async Task<UserLoginAttempt?> FindLoginAttemptAsync(string id, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(id);
        ct.ThrowIfCancellationRequested();

        LoginAttempt? attempt = await LoginAttempts.FindAsync([id], ct).AsTask();
        return attempt?.Adapt<UserLoginAttempt>(_loginAttemptAdaptConfig);
    }

    public async Task<TUser?> FindUserByLoginAttemptAsync(UserLoginAttempt attempt, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(attempt);
        ct.ThrowIfCancellationRequested();

        LoginAttempt? dbAttempt = await LoginAttempts.FindAsync([attempt.Id], ct);
        if (dbAttempt is not null)
        {
            return await FindUserAsync(dbAttempt.UserId, ct);
        }

        return null;
    }

    public async Task<IEnumerable<UserLoginAttempt>> FindLoginAttemptsByUserAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        return await LoginAttempts
            .Where(attempt => attempt.UserId.Equals(userId))
            .ProjectToType<UserLoginAttempt>(_loginAttemptAdaptConfig)
            .ToListAsync(ct);
    }

    public async Task AddLoginAttemptAsync(TUser user, UserLoginAttempt attempt, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(attempt);
        ct.ThrowIfCancellationRequested();

        LoginAttempt dbDto = attempt.Adapt<LoginAttempt>(_loginAttemptAdaptConfig);
        dbDto.UserId = await GetUserIdAsync(user, ct);
        await LoginAttempts.AddAsync(dbDto, ct).AsTask();
    }

    public async Task RemoveLoginAttemptAsync(UserLoginAttempt attempt, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(attempt);
        ct.ThrowIfCancellationRequested();

        await Credentials.Where(dto => dto.Id.Equals(dto.Id)).ExecuteDeleteAsync(ct);
    }

    public async Task RemoveAllLoginAttemptsAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        await LoginAttempts.Where(attempt => attempt.UserId.Equals(userId)).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Store.Enums.LoginAttemptMethod, DateTime>> GetLatestLoginMethodUseTimeAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        IEnumerable<dynamic> attempts = await LoginAttempts
            .AsNoTracking()
            .Select(attempt => new { attempt.UserId, attempt.Method, attempt.Result, attempt.DateTime })
            .Where(attempt => attempt.UserId.Equals(userId))
            .Where(attempt => attempt.Result == LoginAttemptResult.Succeeded || attempt.Result == LoginAttemptResult.TwoFactorRequired)
            .GroupBy(attempt => attempt.Method).Select(group => group.OrderByDescending(attempt => attempt.DateTime).First())     // DistinctBy isn't currently supported by ef core. The expression in this line does the same.
            .ToListAsync(ct);

        return attempts.ToDictionary<dynamic, Store.Enums.LoginAttemptMethod, DateTime>(
            keySelector: attempt => (Store.Enums.LoginAttemptMethod)attempt.Method,
            elementSelector: attempt => attempt.DateTime);
    }
    #endregion
}