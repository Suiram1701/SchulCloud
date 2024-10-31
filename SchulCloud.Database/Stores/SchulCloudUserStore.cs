using Fido2NetLib;
using Fido2NetLib.Objects;
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
        if (credential is not null)
        {
            return ToUserCredential(credential);
        }

        return null;
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
        IEnumerable<Credential> credentials = await Credentials
            .AsNoTracking()
            .Where(cred => cred.UserId.Equals(userId))
            .ToListAsync(ct);
        return credentials.Select(ToUserCredential);
    }

    public async Task AddCredentialAsync(TUser user, UserCredential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        Credential dbCredential = new()
        {
            Id = credential.Id,
            UserId = await GetUserIdAsync(user, ct),
            Name = credential.Name,
            PublicKey = credential.PublicKey,
            SignCount = credential.SignCount,
            Transports = credential.Transports,
            IsBackupEligible = credential.IsBackupEligible,
            IsBackedUp = credential.IsBackedUp,
            AttestationObject = credential.AttestationObject,
            AttestationClientDataJson = credential.AttestationClientDataJson,
            AttestationFormat = credential.AttestationFormat,
            RegDate = credential.RegDate,
            AaGuid = credential.AaGuid
        };
        await Credentials.AddAsync(dbCredential, ct).AsTask();
    }

    public async Task UpdateCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        Credential? dbCredential = await Credentials.FindAsync([credential.Id], ct).AsTask();
        if (dbCredential is not null)
        {
            dbCredential.Name = credential.Name;
            dbCredential.PublicKey = credential.PublicKey;
            dbCredential.SignCount = credential.SignCount;
            dbCredential.Transports = credential.Transports;
            dbCredential.IsBackupEligible = credential.IsBackupEligible;
            dbCredential.IsBackedUp = credential.IsBackedUp;
            dbCredential.AttestationObject = credential.AttestationObject;
            dbCredential.AttestationClientDataJson = credential.AttestationClientDataJson;
            dbCredential.AttestationFormat = credential.AttestationFormat;
            dbCredential.AaGuid = credential.AaGuid;
        }
        else
        {
            throw new InvalidOperationException($"Not entity found with id {string.Join('-', credential.Id)}.");
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

    private static UserCredential ToUserCredential(Credential credential)
    {
        return new()
        {
            Id = credential.Id,
            Name = credential.Name,
            PublicKey = credential.PublicKey,
            SignCount = credential.SignCount,
            Transports = credential.Transports,
            IsBackupEligible = credential.IsBackupEligible,
            IsBackedUp = credential.IsBackedUp,
            AttestationObject = credential.AttestationObject,
            AttestationClientDataJson = credential.AttestationClientDataJson,
            AttestationFormat = credential.AttestationFormat,
            RegDate = credential.RegDate,
            AaGuid = credential.AaGuid
        };
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

    #region IUserLogInAttemptStore
    public async Task<UserLoginAttempt?> FindLoginAttemptAsync(string id, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(id);
        ct.ThrowIfCancellationRequested();

        LoginAttempt? attempt = await LoginAttempts.FindAsync([id], ct).AsTask();
        if (attempt is not null)
        {
            return ToUserAttempt(attempt);
        }

        return null;
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
        IEnumerable<LoginAttempt> attempts = await LoginAttempts
            .AsNoTracking()
            .Where(attempt => attempt.UserId.Equals(userId))
            .ToListAsync(ct);
        return attempts.Select(ToUserAttempt);
    }

    public async Task AddLoginAttemptAsync(TUser user, UserLoginAttempt attempt, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(attempt);
        ct.ThrowIfCancellationRequested();

        LoginAttempt dbAttempt = new()
        {
            UserId = await GetUserIdAsync(user, ct),
            Method = (LoginAttemptMethod)attempt.Method,
            Result = (LoginAttemptResult?)attempt.Result,
            IpAddress = attempt.IpAddress.GetAddressBytes(),
            Latitude = attempt.Latitude,
            Longitude = attempt.Longitude,
            UserAgent = attempt.UserAgent,
            DateTime = attempt.DateTime
        };
        await LoginAttempts.AddAsync(dbAttempt, ct).AsTask();
    }

    public async Task RemoveLoginAttemptAsync(UserLoginAttempt attempt, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(attempt);
        ct.ThrowIfCancellationRequested();

        await LoginAttempts.Where(dbAttempt => dbAttempt.Id.Equals(attempt.Id)).ExecuteDeleteAsync(ct);
    }

    public async Task RemoveAllLoginAttemptsAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        await LoginAttempts.Where(attempt => attempt.UserId.Equals(userId)).ExecuteDeleteAsync(ct);
    }

    private static UserLoginAttempt ToUserAttempt(LoginAttempt attempt)
    {
        return new()
        {
            Id = attempt.Id,
            Method = (Store.Enums.LoginAttemptMethod)attempt.Method,
            Result = (Store.Enums.LoginAttemptResult?)attempt.Result,
            IpAddress = new(attempt.IpAddress),
            Latitude = attempt.Latitude,
            Longitude = attempt.Longitude,
            UserAgent = attempt.UserAgent,
            DateTime = attempt.DateTime
        };
    }
    #endregion
}