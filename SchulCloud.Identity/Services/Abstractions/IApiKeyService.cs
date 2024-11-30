using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Identity.Services.Abstractions;

/// <summary>
/// A service that provides methods for api keys.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a new api key for the specified user.
    /// </summary>
    /// <remarks>
    /// This will only generate the key but does not save it into the database.
    /// </remarks>
    /// <returns>The new generated key.</returns>
    public string GenerateNewApiKey();

    /// <summary>
    /// Hashes an api key.
    /// </summary>
    /// <remarks>
    /// This can be used to hash a recently generated key or to hash a key that should be validated.
    /// </remarks>
    /// <param name="key">The key to hash.</param>
    /// <returns>The hash of the key.</returns>
    public string HashApiKey(string key);
}
