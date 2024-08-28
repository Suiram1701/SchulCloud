using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database.Enums;
using SchulCloud.Database.Models;
using SchulCloud.Store.Abstractions;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;

namespace SchulCloud.Database.Stores;

public class SchulCloudUserStore<TUser, TRole, TContext>(TContext context, IdentityErrorDescriber? describer = null)
    : UserStore<TUser, TRole, TContext>(context, describer),
    IUserFido2CredentialStore<Fido2Credential, TUser>,
    IUserTwoFactorEmailStore<TUser>,
    IUserTwoFactorSecurityKeyStore<TUser>
    where TUser : SchulCloudUser
    where TRole : IdentityRole
    where TContext : DbContext
{
    private DbSet<Fido2Credential> Credentials => Context.Set<Fido2Credential>();

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
    #endregion

    #region IUserFido2CredentialStore
    public async Task<Fido2User> UserToFido2UserAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        string userId = await GetUserIdAsync(user, ct);
        string? userName = await GetUserNameAsync(user, ct);
        string? userEmail = await GetEmailAsync(user, ct);

        return new Fido2User()
        {
            Id = Guid.Parse(userId).ToByteArray(),
            DisplayName = userName,
            Name = userEmail,
        };
    }

    public async Task<Fido2Credential?> GetCredentialById(byte[] id, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(id);

        return await Credentials.FindAsync([id], cancellationToken: ct).AsTask();
    }

    public async Task<IEnumerable<Fido2Credential>> GetCredentialsByUserAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        string userId = await GetUserIdAsync(user, ct);
        return await Credentials.Where(cred => cred.UserId.Equals(userId)).ToArrayAsync(ct);
    }

    public Task<PublicKeyCredentialDescriptor> GetCredentialDescriptorAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        PublicKeyCredentialDescriptor descriptor = new(PublicKeyCredentialType.PublicKey, credential.Id, credential.Transports);
        return Task.FromResult(descriptor);
    }

    public async Task<TUser> GetCredentialOwnerAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        return (await FindUserAsync(credential.UserId, ct))!;
    }

    public Task<string?> GetCredentialSecurityKeyNameAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        return Task.FromResult(credential.SecurityKeyName);
    }

    public Task SetCredentialSecurityKeyNameAsync(Fido2Credential credential, string? newName, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        credential.SecurityKeyName = newName;
        return Task.CompletedTask;
    }

    public Task<bool> GetCredentialIsPasskeyAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        return Task.FromResult(credential.IsPasskey);
    }

    public Task<byte[]> GetCredentialPublicKeyAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        return Task.FromResult(credential.PublicKey);
    }

    public Task<uint> GetCredentialSignCountAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        return Task.FromResult(credential.SignCount);
    }

    public Task SetCredentialSignCountAsync(Fido2Credential credential, uint count, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        credential.SignCount = count;
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<byte[]>> GetCredentialPublicDeviceKeys(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        Fido2Credential cred = await Credentials
            .Include(cred => cred.DevicePublicKeys)
            .SingleAsync(cred => cred.Id.SequenceEqual(credential.Id), ct);
        return cred.DevicePublicKeys.Select(pdk => pdk.PublicDeviceKey);
    }

    public Task AddCredentialPublicDeviceKey(Fido2Credential credential, byte[] publicDeviceKey, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(publicDeviceKey);

        Fido2PublicDeviceKey pdk = new()
        {
            CredentialId = credential.Id,
            PublicDeviceKey = publicDeviceKey,
        };
        credential.DevicePublicKeys.Add(pdk);
        return Task.CompletedTask;
    }

    public Task<DateTime> GetCredentialRegistrationDateAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        return Task.FromResult(credential.RegDate);
    }

    public Task<Guid> GetCredentialAaGuidAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        return Task.FromResult(credential.AaGuid);
    }

    public async Task<Fido2Credential> CreateCredentialAsync(TUser user, string? securityKeyName, bool usernamelessAllowed, RegisteredPublicKeyCredential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(credential);

        string userId = await GetUserIdAsync(user, ct);
        Fido2Credential cred = new()
        {
            Id = credential.Id,
            UserId = userId,
            SecurityKeyName = securityKeyName,
            IsPasskey = usernamelessAllowed,
            PublicKey = credential.PublicKey,
            SignCount = credential.SignCount,
            Transports = credential.Transports,
            IsBackupEligible = credential.IsBackupEligible,
            IsBackedUp = credential.IsBackedUp,
            AttestationObject = credential.AttestationObject,
            AttestationClientDataJson = credential.AttestationClientDataJson,
            AttestationFormat = credential.AttestationFormat,
            RegDate = DateTime.UtcNow,
            AaGuid = credential.AaGuid
        };

        await Credentials.AddAsync(cred, ct).AsTask();
        return cred;
    }

    public Task DeleteCredentialAsync(Fido2Credential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        Credentials.Remove(credential);
        return Task.CompletedTask;
    }

    public async Task<bool> IsCredentialOwnedByUserHandle(byte[] credId, byte[] userHandle, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credId);
        ArgumentNullException.ThrowIfNull(userHandle);
        if (userHandle.Length != 16)
        {
            return false;
        }

        string userId = new Guid(userHandle).ToString();
        return await Credentials.AnyAsync(cred => cred.Id.SequenceEqual(credId) && cred.UserId.Equals(userId), ct);
    }
    #endregion
}