using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchulCloud.Database.Models;
using SchulCloud.Database.Stores;
using SchulCloud.Web.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Web.Identity.Managers;

/// <summary>
/// A user manager that provides functionalities of this application.
/// </summary>
public class SchulCloudUserManager(
    IUserStore<User> store,
    IOptions<IdentityOptions> optionsAccessor,
    IOptions<ExtendedTokenProviderOptions> tokenProviderOptionsAccessor,
    IPasswordHasher<User> passwordHasher,
    IEnumerable<IUserValidator<User>> userValidators,
    IEnumerable<IPasswordValidator<User>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<User>> logger)
    : UserManager<User>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
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
            return Store is IUserTwoFactorEmailStore<User>;
        }
    }

    /// <summary>
    /// Gets the flag whether email two factor is enabled.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The flag.</returns>
    public async Task<bool> GetTwoFactorEmailEnabledAsync(User user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserTwoFactorEmailStore<User> store = GetTwoFactorEmailStore();
        return await store.GetTwoFactorEmailEnabled(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the flag whether email to factor is enabled.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="enabled">The flag.</param>
    /// <returns>The result.</returns>
    public async Task<IdentityResult> SetTwoFactorEmailEnabledAsync(User user, bool enabled)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserTwoFactorEmailStore<User> store = GetTwoFactorEmailStore();
        await store.SetTwoFactorEmailEnabled(user, enabled, CancellationToken).ConfigureAwait(false);

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);     // UpdateSecurityStampAsync also calls UpdateAsync.
    }

    public async Task<string> GenerateTwoFactorEmailCodeAsync(User user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        string provider = ExtendedTokenProviderOptions.EmailTwoFactorTokenProvider;
        return await GenerateTwoFactorTokenAsync(user, provider).ConfigureAwait(false);
    }

    /// <returns>The new recovery codes for the user.</returns>
    /// <inheritdoc />
    public override async Task<IEnumerable<string>?> GenerateNewTwoFactorRecoveryCodesAsync(User user, int number)
    {
        ThrowIfDisposed();
        IUserTwoFactorRecoveryCodeStore<User> store = GetTwoFactorRecoveryCodeStore();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfNegative(number);

        HashSet<string> codes = new(number);
        while (codes.Count < number)
        {
            codes.Add(CreateTwoFactorRecoveryCode());
        }

        await store.ReplaceCodesAsync(user, HashRecoveryCodes(user, codes), CancellationToken).ConfigureAwait(false);
        IdentityResult result = await UpdateAsync(user).ConfigureAwait(false);

        if (result.Succeeded)
        {
            return codes;
        }
        return null;
    }

    public override Task<IdentityResult> RedeemTwoFactorRecoveryCodeAsync(User user, string code)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        return base.RedeemTwoFactorRecoveryCodeAsync(user, HashRecoveryCode(user, code));
    }

    private static string HashRecoveryCode(User user, string code)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(user.Id);
        byte[] codeBytes = Encoding.UTF8.GetBytes(code);
        byte[] result = HMACSHA256.HashData(keyBytes, codeBytes);
        return Convert.ToBase64String(result);
    }

    private static IEnumerable<string> HashRecoveryCodes(User user, IEnumerable<string> codes)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(user.Id);
        foreach (string code in codes)
        {
            byte[] codeBytes = Encoding.UTF8.GetBytes(code);
            byte[] result = HMACSHA256.HashData(keyBytes, codeBytes);
            yield return Convert.ToBase64String(result);
        }
    }

    private IUserTwoFactorEmailStore<User> GetTwoFactorEmailStore()
    {
        if (Store is not IUserTwoFactorEmailStore<User> cast)
        {
            throw new NotSupportedException($"{nameof(IUserTwoFactorEmailStore<User>)} isn't supported by the store.");
        }
        return cast;
    }

    private IUserTwoFactorRecoveryCodeStore<User> GetTwoFactorRecoveryCodeStore()
    {
        if (Store is not IUserTwoFactorRecoveryCodeStore<User> cast)
        {
            throw new NotSupportedException($"{nameof(IUserTwoFactorRecoveryCodeStore<User>)} isn't supported by the store.");
        }
        return cast;
    }
}
