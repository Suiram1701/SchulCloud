using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;
using SchulCloud.Identity.Options;
using SchulCloud.Identity.Services.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace SchulCloud.Identity.Services;

/// <summary>
/// A default implementation of <see cref="IApiKeyService"/>.
/// </summary>
/// <param name="optionsAccessor">The key options to use.</param>
public class ApiKeyService(IOptionsSnapshot<ApiKeyOptions> optionsAccessor) : IApiKeyService
{
    public ApiKeyOptions Options => optionsAccessor.Value;

    public string GenerateNewApiKey()
    {
        string key = RandomNumberGenerator.GetString(Options.AllowedChars, Options.KeyLength);
        if (!string.IsNullOrWhiteSpace(Options.KeyPrefix))
        {
            return $"{Options.KeyPrefix}-{key}";
        }

        return key;
    }

    public string HashApiKey(string key)
    {
        byte[] salt = Encoding.UTF8.GetBytes(Options.GlobalSalt);
        byte[] hash = KeyDerivation.Pbkdf2(key, salt, KeyDerivationPrf.HMACSHA256, 1000, 32);
        return Convert.ToBase64String(hash);
    }
}
