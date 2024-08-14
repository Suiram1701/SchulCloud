using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchulCloud.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Web.Identity.Managers;

public class SchulCloudUserManager(
    IUserStore<User> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<User> passwordHasher,
    IEnumerable<IUserValidator<User>> userValidators,
    IEnumerable<IPasswordValidator<User>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<User>> logger)
    : UserManager<User>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
    /// <returns>The new recovery codes for the user.</returns>
    /// <inheritdoc />
    public override async Task<IEnumerable<string>?> GenerateNewTwoFactorRecoveryCodesAsync(User user, int number)
    {
        ThrowIfDisposed();
        IUserTwoFactorRecoveryCodeStore<User> store = GetRecoveryCodeStore();
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

    private IUserTwoFactorRecoveryCodeStore<User> GetRecoveryCodeStore()
    {
        if (Store is not IUserTwoFactorRecoveryCodeStore<User> cast)
        {
            throw new NotSupportedException($"{nameof(IUserTwoFactorRecoveryCodeStore<User>)} isn't supported by the store.");
        }
        return cast;
    }
}
