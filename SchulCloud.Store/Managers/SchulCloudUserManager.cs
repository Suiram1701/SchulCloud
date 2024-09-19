using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchulCloud.Store.Abstractions;
using SchulCloud.Store.Options;
using System.Security.Cryptography;
using System.Text;

namespace SchulCloud.Store.Managers;

/// <summary>
/// A user manager that provides functionalities of this application.
/// </summary>
public partial class SchulCloudUserManager<TUser, TCredential>(
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
    where TCredential : class
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
            return Store is IUserPasskeysStore<TUser, TCredential> && SupportsUserFido2Credentials;
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
            return Store is IUserTwoFactorSecurityKeyStore<TUser> && SupportsUserFido2Credentials;
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

        IUserPasskeysStore<TUser, TCredential> store = GetPasskeysStore();
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

        IUserPasskeysStore<TUser, TCredential> store = GetPasskeysStore();
        await store.SetPasskeysEnabledAsync(user, enabled, CancellationToken).ConfigureAwait(false);

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a flag that indicates whether a credential was registered as a passkey.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <returns>The flag.</returns>
    public virtual async Task<bool> GetIsPasskey(TCredential credential)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        IUserPasskeysStore<TUser, TCredential> store = GetPasskeysStore();

        return await store.GetIsPasskeyAsync(credential, CancellationToken).ConfigureAwait(false);
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

        IUserPasskeysStore<TUser, TCredential> store = GetPasskeysStore();
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
            IUserPasskeysStore<TUser, TCredential> passkeysStore = GetPasskeysStore();
            await passkeysStore.SetPasskeysEnabledAsync(user, false, CancellationToken).ConfigureAwait(false);
        }

        if (SupportsUserTwoFactorSecurityKeys)
        {
            IUserTwoFactorSecurityKeyStore<TUser> securityKeyStore = GetTwoFactorSecurityKeyStore();
            await securityKeyStore.SetTwoFactorSecurityKeyEnabledAsync(user, false, CancellationToken).ConfigureAwait(false);
        }

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    private static string HashRecoveryCode(string userId, string code)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(userId);
        byte[] codeBytes = Encoding.UTF8.GetBytes(code);

        return Convert.ToBase64String(HMACSHA256.HashData(keyBytes, codeBytes));
    }

    private IUserPasskeysStore<TUser, TCredential> GetPasskeysStore()
    {
        if (Store is not IUserPasskeysStore<TUser, TCredential> cast)
        {
            throw new NotSupportedException($"{nameof(IUserPasskeysStore<TUser, TCredential>)} isn't supported by the store.");
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
}
