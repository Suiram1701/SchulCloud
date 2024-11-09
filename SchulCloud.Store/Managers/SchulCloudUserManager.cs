using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyCSharp.HttpUserAgentParser;
using MyCSharp.HttpUserAgentParser.Providers;
using SchulCloud.Authorization;
using SchulCloud.Store.Abstractions;
using SchulCloud.Store.Enums;
using SchulCloud.Store.Models;
using SchulCloud.Store.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SchulCloud.Store.Managers;

/// <summary>
/// A user manager that provides functionalities of this application.
/// </summary>
public partial class SchulCloudUserManager<TUser>(
    IUserStore<TUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IOptions<ExtendedTokenProviderOptions> tokenProviderOptionsAccessor,
    IPasswordHasher<TUser> passwordHasher,
    IEnumerable<IUserValidator<TUser>> userValidators,
    IEnumerable<IPasswordValidator<TUser>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<TUser>> logger)
    : UserManager<TUser>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger: logger)
    where TUser : class
{
    private readonly IServiceProvider _services = services;

    /// <summary>
    /// Extended token provider options
    /// </summary>
    public ExtendedTokenProviderOptions ExtendedTokenProviderOptions { get; set; } = tokenProviderOptionsAccessor.Value;

    /// <summary>
    /// Indicate whether the internal store supports passkey sign ins.
    /// </summary>
    public virtual bool SupportsUserPasskeys
    {
        get
        {
            ThrowIfDisposed();
            return Store is IUserPasskeysStore<TUser> && SupportsUserCredentials;
        }
    }

    /// <summary>
    /// Indicates whether the internal store support two factor via email.
    /// </summary>
    public virtual bool SupportsUserTwoFactorEmail
    {
        get
        {
            ThrowIfDisposed();
            return Store is IUserTwoFactorEmailStore<TUser>;
        }
    }

    /// <summary>
    /// Indicates whether the internal store supports two factor via security keys.
    /// </summary>
    public virtual bool SupportsUserTwoFactorSecurityKeys
    {
        get
        {
            ThrowIfDisposed();
            return Store is IUserTwoFactorSecurityKeyStore<TUser> && SupportsUserCredentials;
        }
    }

    /// <summary>
    /// Indicates whether the internal store supports log in attempts.
    /// </summary>
    public virtual bool SupportsUserLoginAttempts
    {
        get
        {
            ThrowIfDisposed();
            return Store is IUserLoginAttemptStore<TUser>;
        }
    }

    /// <summary>
    /// Gets a flag that indicates whether a user has passkey sign ins enabled.
    /// </summary>
    /// <param name="user">The user to get the flag from.</param>
    /// <returns>The flag.</returns>
    public virtual async Task<bool> GetPasskeySignInEnabledAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserPasskeysStore<TUser> store = GetPasskeysStore();
        return await store.GetPasskeysEnabledAsync(user, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the flag that indicates whether a user has passkeys sign ins enabled.
    /// </summary>
    /// <param name="user">The user to modify.</param>
    /// <param name="enabled">The new flag.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> SetPasskeySignInEnabledAsync(TUser user, bool enabled)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserPasskeysStore<TUser> store = GetPasskeysStore();
        await store.SetPasskeysEnabledAsync(user, enabled, CancellationToken).ConfigureAwait(false);

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a flag that indicates whether a credential was registered as a passkey.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <returns>The flag.</returns>
    public virtual async Task<bool> GetIsPasskey(UserCredential credential)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        IUserPasskeysStore<TUser> store = GetPasskeysStore();

        return await store.GetIsPasskeyCredentialAsync(credential, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the count of available passkeys for a user.
    /// </summary>
    /// <param name="user">The user to get this from.</param>
    /// <returns>The count of keys.</returns>
    public virtual async Task<int> GetPasskeyCountAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserPasskeysStore<TUser> store = GetPasskeysStore();
        return await store.GetPasskeyCountAsync(user, CancellationToken).ConfigureAwait(false);
    }

    public override async Task<IdentityResult> SetTwoFactorEnabledAsync(TUser user, bool enabled)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(enabled);

        IUserTwoFactorStore<TUser> twoFactorStore = GetTwoFactorStore();
        await twoFactorStore.SetTwoFactorEnabledAsync(user, enabled, CancellationToken).ConfigureAwait(false);

        if (!enabled)
        {
            // Reset every other 2fa method.
            IUserAuthenticatorKeyStore<TUser> authenticatorKeyStore = GetAuthenticatorKeyStore();
            await authenticatorKeyStore.SetAuthenticatorKeyAsync(user, string.Empty, CancellationToken).ConfigureAwait(false);

            IUserTwoFactorEmailStore<TUser> twoFactorEmailStore = GetTwoFactorEmailStore();
            await twoFactorEmailStore.SetTwoFactorEmailEnabled(user, false, CancellationToken).ConfigureAwait(false);

            IUserTwoFactorSecurityKeyStore<TUser> twoFactorSecurityKeyStore = GetTwoFactorSecurityKeyStore();
            await twoFactorSecurityKeyStore.SetTwoFactorSecurityKeyEnabledAsync(user, false, CancellationToken).ConfigureAwait(false);

            IUserTwoFactorRecoveryCodeStore<TUser> twoFactorRecoveryStore = GetTwoFactorRecoveryCodeStore();
            await twoFactorRecoveryStore.ReplaceCodesAsync(user, [], CancellationToken).ConfigureAwait(false);
        }

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the flag whether email two factor is enabled.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The flag.</returns>
    public virtual async Task<bool> GetTwoFactorEmailEnabledAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserTwoFactorEmailStore<TUser> store = GetTwoFactorEmailStore();
        return await store.GetTwoFactorEmailEnabled(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the flag whether email to factor is enabled.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="enabled">The flag.</param>
    /// <returns>The result.</returns>
    public virtual async Task<IdentityResult> SetTwoFactorEmailEnabledAsync(TUser user, bool enabled)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserTwoFactorEmailStore<TUser> store = GetTwoFactorEmailStore();
        await store.SetTwoFactorEmailEnabled(user, enabled, CancellationToken).ConfigureAwait(false);

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a new two factor email code.
    /// </summary>
    /// <param name="user">The user to generate the code for.</param>
    /// <returns>The generated code.</returns>
    public virtual async Task<string> GenerateTwoFactorEmailCodeAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        string provider = ExtendedTokenProviderOptions.EmailTwoFactorTokenProvider;
        return await GenerateTwoFactorTokenAsync(user, provider).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a flag that indicates whether two factor via security keys is enabled.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The result.</returns>
    public virtual async Task<bool> GetTwoFactorSecurityKeyEnableAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserTwoFactorSecurityKeyStore<TUser> store = GetTwoFactorSecurityKeyStore();
        return await store.GetTwoFactorSecurityKeyEnabledAsync(user, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets tha flag that indicates whether two factor via security keys is enabled.
    /// </summary>
    /// <param name="user">The user to modify.</param>
    /// <param name="enabled">The new flag.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> SetTwoFactorSecurityKeyEnabledAsync(TUser user, bool enabled)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserTwoFactorSecurityKeyStore<TUser> store = GetTwoFactorSecurityKeyStore();
        await store.SetTwoFactorSecurityKeyEnabledAsync(user, enabled, CancellationToken).ConfigureAwait(false);

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the count of registered security keys that can be used for two factor.
    /// </summary>
    /// <param name="user">The user to get the count from.</param>
    /// <returns>The count of keys.</returns>
    public virtual async Task<int> GetTwoFactorSecurityKeysCountAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserTwoFactorSecurityKeyStore<TUser> store = GetTwoFactorSecurityKeyStore();
        return await store.GetTwoFactorSecurityKeyCountAsync(user, CancellationToken).ConfigureAwait(false);
    }

    /// <returns>The new recovery codes for the user.</returns>
    /// <inheritdoc />
    public override async Task<IEnumerable<string>?> GenerateNewTwoFactorRecoveryCodesAsync(TUser user, int number)
    {
        ThrowIfDisposed();
        IUserTwoFactorRecoveryCodeStore<TUser> store = GetTwoFactorRecoveryCodeStore();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfNegative(number);

        HashSet<string> codes = new(number);
        while (codes.Count < number)
        {
            codes.Add(CreateTwoFactorRecoveryCode());
        }

        string userId = await GetUserIdAsync(user);
        IEnumerable<string> hashedCodes = codes.Select(code => HashRecoveryCode(userId, code));

        await store.ReplaceCodesAsync(user, hashedCodes, CancellationToken).ConfigureAwait(false);
        IdentityResult result = await UpdateAsync(user).ConfigureAwait(false);

        return result.Succeeded
            ? codes
            : null;
    }

    public override async Task<IdentityResult> RedeemTwoFactorRecoveryCodeAsync(TUser user, string code)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        string userId = await GetUserIdAsync(user).ConfigureAwait(false);
        return await base.RedeemTwoFactorRecoveryCodeAsync(user, HashRecoveryCode(userId, code));
    }
    
    private static string HashRecoveryCode(string userId, string code)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(userId);
        byte[] codeBytes = Encoding.UTF8.GetBytes(code);

        return Convert.ToBase64String(HMACSHA256.HashData(keyBytes, codeBytes));
    }

    /// <summary>
    /// Disables both Passkey sign in and security key 2fa authentication for a user.
    /// </summary>
    /// <param name="user">The user to disable it for.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> DisableSecurityKeyAuthenticationAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        if (SupportsUserPasskeys)
        {
            IUserPasskeysStore<TUser> passkeysStore = GetPasskeysStore();
            await passkeysStore.SetPasskeysEnabledAsync(user, false, CancellationToken).ConfigureAwait(false);
        }

        if (SupportsUserTwoFactorSecurityKeys)
        {
            IUserTwoFactorSecurityKeyStore<TUser> securityKeyStore = GetTwoFactorSecurityKeyStore();
            await securityKeyStore.SetTwoFactorSecurityKeyEnabledAsync(user, false, CancellationToken).ConfigureAwait(false);
        }

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds a login attempt by its id.
    /// </summary>
    /// <param name="id">The id of the attempt.</param>
    /// <returns>The attempt. If <c>null</c> no attempt with this id were found.</returns>
    public virtual async Task<UserLoginAttempt?> FindLoginAttemptAsync(string id)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(id); 

        IUserLoginAttemptStore<TUser> store = GetLoginAttemptStore();
        return await store.FindLoginAttemptAsync(id, CancellationToken);
    }

    /// <summary>
    /// Finds all login attempts of a user.
    /// </summary>
    /// <param name="user">The user to get these for.</param>
    /// <returns>The log in attempts.</returns>
    public virtual async Task<IEnumerable<UserLoginAttempt>> FindLoginAttemptsByUserAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserLoginAttemptStore<TUser> store = GetLoginAttemptStore();
        return await store.FindLoginAttemptsByUserAsync(user, CancellationToken);
    }

    /// <summary>
    /// Adds a login attempt to a user.
    /// </summary>
    /// <remarks>
    /// This method should only get called by the SignInManager.
    /// </remarks>
    /// <param name="user">The user to add this attempt to.</param>
    /// <param name="methodCode">The code of the log in method used for the attempt.</param>
    /// <param name="succeeded">Was the login successful or not.</param>
    /// <param name="ipAddress">The clients ip address.</param>
    /// <param name="userAgent">The user agent used by the client.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> AddLoginAttemptAsync(TUser user, UserLoginAttempt attempt)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(attempt);

        IUserLoginAttemptStore<TUser> store = GetLoginAttemptStore();
        await store.AddLoginAttemptAsync(user, attempt, CancellationToken);

        return await Store.UpdateAsync(user, CancellationToken);
    }

    /// <summary>
    /// Removes a single login attempt of a user.
    /// </summary>
    /// <param name="user">The user that this attempt is for.</param>
    /// <param name="attempt">The attempt to remove.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> RemoveLoginAttemptAsync(TUser user, UserLoginAttempt attempt)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(attempt);

        IUserLoginAttemptStore<TUser> store = GetLoginAttemptStore();
        await store.RemoveLoginAttemptAsync(attempt, CancellationToken);

        return await Store.UpdateAsync(user, CancellationToken);
    }

    /// <summary>
    /// Removes all login attempts of a user.
    /// </summary>
    /// <param name="user">The user to remove all attempts from.</param>
    /// <returns>The result of operation.</returns>
    public virtual async Task<IdentityResult> RemoveAllLoginAttemptsOfUserAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserLoginAttemptStore<TUser> store = GetLoginAttemptStore();
        await store.RemoveAllLoginAttemptsAsync(user, CancellationToken);

        return await Store.UpdateAsync(user, CancellationToken);
    }

    /// <summary>
    /// Gets the for every login method the last time they were successfully used.
    /// </summary>
    /// <remarks>
    /// This methods depends on <see cref="SupportsUserLoginAttempts"/>.
    /// </remarks>
    /// <param name="user">The user to get these last use times from.</param>
    /// <returns>Key-value-pairs that contains the method and their last use tim. If a method isn't contained the method wasn't used yet.</returns>
    public virtual async Task<IReadOnlyDictionary<LoginAttemptMethod, DateTime>> GetLatestLoginMethodUseTimeOfUserAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserLoginAttemptStore<TUser> store = GetLoginAttemptStore();
        return await store.GetLatestLoginMethodUseTimeAsync(user, CancellationToken);
    }

    /// <summary>
    /// Finds the user that owns a login attempt.
    /// </summary>
    /// <param name="attempt">The attempt to check.</param>
    /// <returns>The user.</returns>
    public virtual async Task<TUser?> FindUserByLoginAttemptAsync(UserLoginAttempt attempt)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(attempt);

        IUserLoginAttemptStore<TUser> store = GetLoginAttemptStore();
        return await store.FindUserByLoginAttemptAsync(attempt, CancellationToken);
    }

    /// <summary>
    /// Sets the permission level of a user for a permission type.
    /// </summary>
    /// <param name="user">The user to the set the permission for.</param>
    /// <param name="permissionName">The name of the permission type,</param>
    /// <param name="level">The permission level to set.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> SetPermissionLevelAsync(TUser user, string permissionName, PermissionLevel level)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);

        IUserPermissionStore<TUser> store = GetPermissionsStore();
        await store.SetPermissionLevel(user, permissionName, level, CancellationToken);

        return await UpdateAsync(user);
    }

    /// <summary>
    /// Gets the level of a permission type of a user.
    /// </summary>
    /// <param name="user">The user to get the permission from.</param>
    /// <param name="permissionName">The name of the permission type.</param>
    /// <returns>The level of the permission. If no level were set for the permission type <see cref="PermissionLevel.None"/> will be returned.</returns>
    public virtual async Task<PermissionLevel> GetPermissionLevelAsync(TUser user, string permissionName)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);

        IUserPermissionStore<TUser> store = GetPermissionsStore();
        return await store.GetPermissionLevel(user, permissionName, CancellationToken);
    }

    private IUserPasskeysStore<TUser> GetPasskeysStore()
    {
        if (Store is not IUserPasskeysStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserPasskeysStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IUserAuthenticatorKeyStore<TUser> GetAuthenticatorKeyStore()
    {
        if (Store is not IUserAuthenticatorKeyStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserAuthenticatorKeyStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IUserTwoFactorStore<TUser> GetTwoFactorStore()
    {
        if (Store is not IUserTwoFactorStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserTwoFactorStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IUserTwoFactorEmailStore<TUser> GetTwoFactorEmailStore()
    {
        if (Store is not IUserTwoFactorEmailStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserTwoFactorEmailStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IUserTwoFactorSecurityKeyStore<TUser> GetTwoFactorSecurityKeyStore()
    {
        if (Store is not IUserTwoFactorSecurityKeyStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserTwoFactorSecurityKeyStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IUserTwoFactorRecoveryCodeStore<TUser> GetTwoFactorRecoveryCodeStore()
    {
        if (Store is not IUserTwoFactorRecoveryCodeStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserTwoFactorRecoveryCodeStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IUserLoginAttemptStore<TUser> GetLoginAttemptStore()
    {
        if (Store is not IUserLoginAttemptStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserLoginAttemptStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IUserPermissionStore<TUser> GetPermissionsStore()
    {
        if (Store is not IUserPermissionStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserPermissionStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }
}
