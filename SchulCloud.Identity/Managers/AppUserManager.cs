using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyCSharp.HttpUserAgentParser;
using MyCSharp.HttpUserAgentParser.Providers;
using SchulCloud.Authorization;
using SchulCloud.Identity.Abstractions;
using SchulCloud.Identity.Enums;
using SchulCloud.Identity.Models;
using SchulCloud.Identity.Options;
using SchulCloud.Identity.Services.Abstractions;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SchulCloud.Store.Managers;

/// <summary>
/// A user manager that provides functionalities of this application.
/// </summary>
public partial class AppUserManager<TUser>(
    IUserStore<TUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IOptions<ExtendedTokenProviderOptions> tokenProviderOptionsAccessor,
    IOptions<ApiKeyOptions> apiKeyOptionsAccessor,
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
    public ExtendedTokenProviderOptions ExtendedTokenProviderOptions { get; } = tokenProviderOptionsAccessor.Value;

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
    /// Indicates whether the internal store supports user api keys.
    /// </summary>
    public virtual bool SupportsUserApiKeys
    {
        get
        {
            ThrowIfDisposed();
            return Store is IUserApiKeyStore<TUser> && _services.GetService<IApiKeyService>() is not null;
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
    /// <param name="permission">The permission to set. If a permission of the same type already exists it will be overridden.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> SetPermissionLevelAsync(TUser user, Permission permission)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(permission);

        IUserPermissionStore<TUser> store = GetPermissionsStore();
        await store.SetPermissionLevelAsync(user, permission, CancellationToken);

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
        return await store.GetPermissionLevelAsync(user, permissionName, CancellationToken);
    }

    /// <summary>
    /// Gets every permission and its level of a user.
    /// </summary>
    /// <param name="user">The user to get the permissions for.</param>
    /// <returns>A dictionary of permissions.</returns>
    public virtual async Task<IReadOnlyCollection<Permission>> GetPermissionLevelsAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserPermissionStore<TUser> store = GetPermissionsStore();
        return await store.GetPermissionLevelsAsync(user, CancellationToken);
    }

    /// <summary>
    /// Finds an api key by its id.
    /// </summary>
    /// <param name="id">The id of the api key.</param>
    /// <returns>The key. If <c>null</c> no key belongs to this id.</returns>
    public virtual async Task<UserApiKey?> FindApiKeyById(string id)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        IUserApiKeyStore<TUser> store = GetApiKeyStore();
        return await store.FindApiKeyByIdAsync(id, CancellationToken);
    }

    /// <summary>
    /// Tries to find an api key and its owner by the raw key.
    /// </summary>
    /// <param name="apiKey">The raw api key.</param>
    /// <returns>The api key and its owner. If <c>null</c> no such was found.</returns>
    public virtual async Task<(UserApiKey key, TUser user)?> FindApiKeyAsync(string apiKey)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(apiKey);
        IApiKeyService apiKeyService = _services.GetRequiredService<IApiKeyService>();

        IUserApiKeyStore<TUser> store = GetApiKeyStore();
        UserApiKey? key = await store.FindApiKeyByKeyHashAsync(apiKeyService.HashApiKey(apiKey), CancellationToken);
        
        if (key is not null)
        {
            TUser user = (await store.FindUserByApiKeyAsync(key, CancellationToken))!;
            return (key, user);
        }

        return null;
    }

    /// <summary>
    /// Finds the owner of an api key.
    /// </summary>
    /// <param name="apiKey">The key to find the owner from.</param>
    /// <returns>The owner of the key. If <c>null</c> the owner wasn't found.</returns>
    public virtual async Task<TUser?> FindUserByApiKeyAsync(UserApiKey apiKey)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(apiKey);

        IUserApiKeyStore<TUser> store = GetApiKeyStore();
        return await store.FindUserByApiKeyAsync(apiKey, CancellationToken);
    }

    /// <summary>
    /// Gets the api keys of a user.
    /// </summary>
    /// <param name="user">The user to get the key from.</param>
    /// <returns>The keys found for the user.</returns>
    public virtual async Task<UserApiKey[]> GetApiKeysByUserAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserApiKeyStore<TUser> store = GetApiKeyStore();
        return await store.GetApiKeysByUserAsync(user, CancellationToken);
    }

    /// <summary>
    /// Adds an api key to a user's account.
    /// </summary>
    /// <param name="user">The user that should own the key.</param>
    /// <param name="apiKey">The api key to add.</param>
    /// <returns>The raw key that will be associated with this user and key. If <c>null</c> an error occurred while adding the key.</returns>
    public virtual async Task<string?> AddApiKeyToUserAsync(TUser user, UserApiKey apiKey)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(apiKey);

        IApiKeyService apiKeyService = _services.GetRequiredService<IApiKeyService>();
        ApiKeyOptions options = apiKeyOptionsAccessor.Value;

        IUserApiKeyStore<TUser> keyStore = GetApiKeyStore();
        int keysCount = (await keyStore.GetApiKeysByUserAsync(user, CancellationToken)).Length;
        if (options.MaxKeysPerUser != -1 && keysCount >= options.MaxKeysPerUser)
        {
            string? userId = await GetUserIdAsync(user);
            Logger.LogInformation("User '{userId}' tried to create more api keys than the maximum count per user. The maximum count is {maxCount}.", userId, options.MaxKeysPerUser);

            return null;
        }

        IUserPermissionStore<TUser> permissionStore = GetPermissionsStore();
        IReadOnlyCollection<Permission> userPermissions = await permissionStore.GetPermissionLevelsAsync(user, CancellationToken);
        bool validPermissions = apiKey.PermissionLevels.All(keyPermission =>
        {
            Permission? userPermission = userPermissions.FirstOrDefault(permission => permission.Name == keyPermission.Name);
            return userPermission?.Level >= keyPermission.Level;
        });
        if (!validPermissions)
        {
            string? userId = await GetUserIdAsync(user);
            Logger.LogInformation("User '{userId}' tried to create an api key with higher privileges than the user it self.", userId);

            return null;
        }

        string key = apiKeyService.GenerateNewApiKey();
        apiKey.KeyHash = apiKeyService.HashApiKey(key);
        apiKey.Created = DateTime.UtcNow;

        await keyStore.AddApiKeyAsync(user, apiKey, CancellationToken);

        IdentityResult result = await UpdateSecurityStampAsync(user);
        return result.Succeeded ? key : null;
    }

    /// <summary>
    /// Removes an api from a user's account.
    /// </summary>
    /// <param name="user">The user that owns the key.</param>
    /// <param name="apiKey">The key to remove.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> RemoveApiKeyAsync(TUser user, UserApiKey apiKey)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(apiKey);

        IUserApiKeyStore<TUser> store = GetApiKeyStore();
        await store.RemoveApiKeyAsync(apiKey, CancellationToken);

        return await UpdateSecurityStampAsync(user);
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

    private IUserApiKeyStore<TUser> GetApiKeyStore()
    {
        if (Store is not IUserApiKeyStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserApiKeyStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }
}
