using SchulCloud.Store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Store.Abstractions;

/// <summary>
/// A store interface that provides api keys for users.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IUserApiKeyStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Finds an api key by its id.
    /// </summary>
    /// <param name="id">The id of key to find.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The api key. If <c>null</c> no such key was found.</returns>
    public Task<UserApiKey?> FindApiKeyByIdAsync(string id, CancellationToken ct);

    /// <summary>
    /// Tries to find an api key by its unique key hash.
    /// </summary>
    /// <param name="keyHash">The hash of the api key.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The found api key. If <c>null</c> no matching key was found.</returns>
    public Task<UserApiKey?> FindApiKeyByKeyHashAsync(string keyHash, CancellationToken ct);

    /// <summary>
    /// Finds a user by an api key owned by him.
    /// </summary>
    /// <param name="apiKey">The api key to use to identify the user.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The owner of the key. If <c>null</c> the user wasn't found.</returns>
    public Task<TUser?> FindUserByApiKeyAsync(UserApiKey apiKey, CancellationToken ct);

    /// <summary>
    /// Gets all api keys owned by a user.
    /// </summary>
    /// <param name="user">The user to get the keys from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The api keys of the user.</returns>
    public Task<UserApiKey[]> GetApiKeysByUserAsync(TUser user, CancellationToken ct);

    /// <summary>
    /// Adds an api key for a user
    /// </summary>
    /// <param name="user">The owner of the key.</param>
    /// <param name="apiKey">The key to store.</param>
    /// <param name="ct">Cancellation token</param>
    public Task AddApiKeyAsync(TUser user, UserApiKey apiKey, CancellationToken ct);

    /// <summary>
    /// Removes an api key.
    /// </summary>
    /// <param name="apiKey">The api key to remove.</param>
    /// <param name="ct">Cancellation token</param>
    public Task RemoveApiKeyAsync(UserApiKey apiKey, CancellationToken ct);
}
