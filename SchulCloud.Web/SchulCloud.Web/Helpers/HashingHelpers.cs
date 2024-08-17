using System.Security.Cryptography;
using System.Text;

namespace SchulCloud.Web.Helpers;

/// <summary>
/// Provides some helpers.
/// </summary>
internal static class HashingHelpers
{
    /// <summary>
    /// Hashes the value by a specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The hash.</returns>
    public static string HashData(string key, string value)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] codeBytes = Encoding.UTF8.GetBytes(value);

        return Convert.ToBase64String(HMACSHA256.HashData(keyBytes, codeBytes));
    }
}
