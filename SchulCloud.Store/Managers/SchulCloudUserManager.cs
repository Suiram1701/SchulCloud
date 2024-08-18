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
public class SchulCloudUserManager<TUser>(
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
    : UserManager<TUser>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    where TUser : class
{
    /// <summary>
    /// Extended token provider options
    /// </summary>
    public ExtendedTokenProviderOptions ExtendedTokenProviderOptions { get; set; } = tokenProviderOptionsAccessor.Value;

    /// <summary>
    /// Indicates whether the store support two factor email.
    /// </summary>
    public bool SupportsUserTwoFactorEmail
    {
        get
        {
            ThrowIfDisposed();
            return Store is IUserTwoFactorEmailStore<TUser>;
        }
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
    public async Task<bool> GetTwoFactorEmailEnabledAsync(TUser user)
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
    public async Task<IdentityResult> SetTwoFactorEmailEnabledAsync(TUser user, bool enabled)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserTwoFactorEmailStore<TUser> store = GetTwoFactorEmailStore();
        await store.SetTwoFactorEmailEnabled(user, enabled, CancellationToken).ConfigureAwait(false);

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    public async Task<string> GenerateTwoFactorEmailCodeAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        string provider = ExtendedTokenProviderOptions.EmailTwoFactorTokenProvider;
        return await GenerateTwoFactorTokenAsync(user, provider).ConfigureAwait(false);
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

    private IUserTwoFactorRecoveryCodeStore<TUser> GetTwoFactorRecoveryCodeStore()
    {
        if (Store is not IUserTwoFactorRecoveryCodeStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserTwoFactorRecoveryCodeStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }
}
