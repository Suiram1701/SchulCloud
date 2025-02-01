using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchulCloud.Authorization;
using SchulCloud.FileStorage.Abstractions;
using SchulCloud.Identity.Abstractions;
using SchulCloud.Identity.Enums;
using SchulCloud.Identity.Models;
using SchulCloud.Identity.Options;
using SchulCloud.Identity.Services.Abstractions;
using SixLabors.ImageSharp;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SchulCloud.Identity.Managers;

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
    public virtual bool SupportsUserPasskeys => SupportsStore<IUserPasskeysStore<TUser>>() && SupportsUserCredentials;

    /// <summary>
    /// Indicates whether the internal store support two factor via email.
    /// </summary>
    public virtual bool SupportsUserTwoFactorEmail => SupportsStore<IUserTwoFactorEmailStore<TUser>>();

    /// <summary>
    /// Indicates whether the internal store supports two factor via security keys.
    /// </summary>
    public virtual bool SupportsUserTwoFactorSecurityKeys => SupportsStore<IUserTwoFactorSecurityKeyStore<TUser>>();

    /// <summary>
    /// Indicates whether the internal store supports log in attempts.
    /// </summary>
    public virtual bool SupportsUserLoginAttempts => SupportsStore<IUserLoginAttemptStore<TUser>>();

    /// <summary>
    /// Indicates whether the internal store supports language settings.
    /// </summary>
    public virtual bool SupportsUserLanguages => SupportsStore<IUserLanguageStore<TUser>>();

    /// <summary>
    /// Indicates whether the internal store supports color theme settings.
    /// </summary>
    public virtual bool SupportsUserColorThemes => SupportsStore<IUserColorThemeStore<TUser>>();

    /// <summary>
    /// Indicates whether the internal store supports user api keys.
    /// </summary>
    public virtual bool SupportsUserApiKeys => SupportsStore<IUserApiKeyStore<TUser>>();

    /// <summary>
    /// Indicates whether the internal store supports profile images.
    /// </summary>
    public virtual bool SupportsProfileImages => _services.GetService<IProfileImageStore<TUser>>() is not null;

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
    /// Sets the culture of a user in which data will be shown.
    /// </summary>
    /// <param name="user">The user to update the culture for.</param>
    /// <param name="culture">The new culture.</param>
    /// <returns>The result of this operation.</returns>
    public virtual async Task<IdentityResult> SetCultureAsync(TUser user, CultureInfo? culture)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserLanguageStore<TUser> store = GetLanguageStore();
        await store.SetCultureAsync(user, culture, CancellationToken);
        return await UpdateAsync(user);
    }

    /// <summary>
    /// Sets the ui culture of a user.
    /// </summary>
    /// <param name="user">The user to update the ui culture for.</param>
    /// <param name="culture">The new ui culture.</param>
    /// <returns>The result of this operation.</returns>
    public virtual async Task<IdentityResult> SetUiCultureAsync(TUser user, CultureInfo? culture)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserLanguageStore<TUser> store = GetLanguageStore();
        await store.SetUiCultureAsync(user, culture, CancellationToken);
        return await UpdateAsync(user);
    }

    /// <summary>
    /// Gets the current culture of a user in which data will be shown.
    /// </summary>
    /// <param name="user">The user who's culture should retrieved.</param>
    /// <returns>The culture. If <c>null</c> it wasn't set yet.</returns>
    public virtual async Task<CultureInfo?> GetCultureAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserLanguageStore<TUser> store = GetLanguageStore();
        return await store.GetCultureAsync(user, CancellationToken);
    }

    /// <summary>
    /// Sets the ui culture of a user.
    /// </summary>
    /// <param name="user">The user for that the ui culture should be retrieved</param>
    /// <returns>The ui culture. If <c>null</c> it wasn't set yet.</returns>
    public virtual async Task<CultureInfo?> GetUiCultureAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserLanguageStore<TUser> store = GetLanguageStore();
        return await store.GetUiCultureAsync(user, CancellationToken);
    }

    /// <summary>
    /// Sets the displayed color theme of a certain user.
    /// </summary>
    /// <param name="user">The user to set the theme for.</param>
    /// <param name="theme">The color theme to set.</param>
    /// <returns>The result of this operation.</returns>
    public virtual async Task<IdentityResult> SetColorThemeAsync(TUser user, ColorTheme theme)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserColorThemeStore<TUser> store = GetColorThemeStore();
        if (theme != ColorTheme.Auto)
        {
            await store.SetColorThemeAsync(user, theme, CancellationToken);
        }
        else
        {
            await store.SetColorThemeAsync(user, null, CancellationToken);
        }
        
        return await UpdateAsync(user);
    }

    /// <summary>
    /// Gets the displayed color theme of a certain user.
    /// </summary>
    /// <param name="user">The user to get the theme from.</param>
    /// <returns>The color theme. If it were not set yet <see cref="ColorTheme.Auto"/> will be returned.</returns>
    public virtual async Task<ColorTheme> GetColorThemeAsync(TUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        IUserColorThemeStore<TUser> store = GetColorThemeStore();
        return await store.GetColorThemeAsync(user, CancellationToken) ?? ColorTheme.Auto;
    }

    /// <summary>
    /// Gets the color theme of the currently authenticated user by his clams principal.
    /// </summary>
    /// <param name="principal">The claims principal of the user.</param>
    /// <returns>The color theme saved in the principal.</returns>
    public virtual ColorTheme GetColorTheme(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        string? claimValue = principal.FindFirstValue("Setting:ColorTheme");

        return !string.IsNullOrEmpty(claimValue) ? Enum.Parse<ColorTheme>(claimValue) : ColorTheme.Auto;
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

    /// <summary>
    /// Retrieves the profile image of a certain user.
    /// </summary>
    /// <param name="user">The user to get the profile image of.</param>
    /// <returns>The profile image. If <c>null</c> not image is available.</returns>
    public virtual async Task<Stream?> GetProfileImageAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IProfileImageStore<TUser> store = _services.GetRequiredService<IProfileImageStore<TUser>>();
        return await store.GetImageAsync(user, CancellationToken);
    }

    /// <summary>
    /// Updates the profile image of a certain user.
    /// </summary>
    /// <remarks>
    /// The image will be automatically converted into PNG format. Acceptable formats are PNG, QOI, PBM, BMP, WebP, JPEG, GIF, TGA and TIFF.
    /// If none of the acceptable formats could be read from the stream an IdentityError with code <c>Bad Image</c> will be returned.
    /// </remarks>
    /// <param name="user">The user to update the image of.</param>
    /// <param name="image">The new image to set.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> UpdateProfileImageAsync(TUser user, Stream image)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(image);

        using MemoryStream pngStream = new();
        try
        {
            using Image img = await Image.LoadAsync(image, CancellationToken).ConfigureAwait(false);
            await img.SaveAsPngAsync(pngStream, CancellationToken).ConfigureAwait(false);
            pngStream.Seek(0, SeekOrigin.Begin);
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex, "Could not load image.");

            return IdentityResult.Failed(new IdentityError
            {
                Code = "Bad Image",
                Description = "Invalid image format"
            });
        }

        IProfileImageStore<TUser> store = _services.GetRequiredService<IProfileImageStore<TUser>>();
        await store.UpdateImageAsync(user, pngStream, CancellationToken).ConfigureAwait(false);

        return IdentityResult.Success;
    }

    /// <summary>
    /// Removes the profile image of a certain user.
    /// </summary>
    /// <param name="user">The user to remove the image of.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> RemoveProfileImageAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IProfileImageStore<TUser> store = _services.GetRequiredService<IProfileImageStore<TUser>>();
        await store.RemoveImageAsync(user, CancellationToken).ConfigureAwait(false);

        return IdentityResult.Success;
    }

    private IUserPasskeysStore<TUser> GetPasskeysStore() => GetStoreBase<IUserPasskeysStore<TUser>>();

    private IUserAuthenticatorKeyStore<TUser> GetAuthenticatorKeyStore() => GetStoreBase<IUserAuthenticatorKeyStore<TUser>>();

    private IUserTwoFactorStore<TUser> GetTwoFactorStore() => GetStoreBase<IUserTwoFactorStore<TUser>>();

    private IUserTwoFactorEmailStore<TUser> GetTwoFactorEmailStore() => GetStoreBase<IUserTwoFactorEmailStore<TUser>>();

    private IUserTwoFactorSecurityKeyStore<TUser> GetTwoFactorSecurityKeyStore() => GetStoreBase<IUserTwoFactorSecurityKeyStore<TUser>>();

    private IUserTwoFactorRecoveryCodeStore<TUser> GetTwoFactorRecoveryCodeStore() => GetStoreBase<IUserTwoFactorRecoveryCodeStore<TUser>>();

    private IUserLoginAttemptStore<TUser> GetLoginAttemptStore() => GetStoreBase<IUserLoginAttemptStore<TUser>>();

    private IUserPermissionStore<TUser> GetPermissionsStore() => GetStoreBase<IUserPermissionStore<TUser>>();

    private IUserLanguageStore<TUser> GetLanguageStore() => GetStoreBase<IUserLanguageStore<TUser>>();

    private IUserColorThemeStore<TUser> GetColorThemeStore() => GetStoreBase<IUserColorThemeStore<TUser>>();

    private IUserApiKeyStore<TUser> GetApiKeyStore() => GetStoreBase<IUserApiKeyStore<TUser>>();

    private bool SupportsStore<TStore>()
    {
        ThrowIfDisposed();
        return Store is TStore;
    }

    private TStore GetStoreBase<TStore>()
    {
        if (Store is not TStore cast)
        {
            throw new NotSupportedException($"{nameof(TStore)} isn't supported by the store.");
        }
        return cast;
    }
}