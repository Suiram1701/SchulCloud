using Fido2NetLib;
using Fido2NetLib.Objects;
using SchulCloud.Store.Models;

namespace SchulCloud.Store.Abstractions;

/// <summary>
/// A store interface that provides fido2 credentials for users.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IUserCredentialStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Finds the credential that has a specified id.
    /// </summary>
    /// <param name="id">The id to find.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The credential. <c>null</c> if not credential with the id was found.</returns>
    public Task<UserCredential?> FindCredentialAsync(byte[] id, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by a credential owned by him.
    /// </summary>
    /// <param name="credential">The credential to get the user from.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user found for this credential.</returns>
    public Task<TUser?> FindUserByCredentialAsync(UserCredential credential, CancellationToken ct = default);

    /// <summary>
    /// Finds every credential that is owned by the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user to find credentials for.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The credentials.</returns>
    public Task<IEnumerable<UserCredential>> FindCredentialsByUserAsync(TUser user, CancellationToken ct = default);

    /// <summary>
    /// Adds a credential to a user.
    /// </summary>
    /// <param name="user">The user that should own the added credential.</param>
    /// <param name="credential">The credential to add.</param>
    /// <param name="ct">Cancellation token</param>
    public Task AddCredentialAsync(TUser user, UserCredential credential, CancellationToken ct = default);

    /// <summary>
    /// Updates a credential.
    /// </summary>
    /// <param name="credential">The new credential.</param>
    /// <param name="ct">Cancellation token</param>
    public Task UpdateCredentialAsync(UserCredential credential, CancellationToken ct = default);

    /// <summary>
    /// Removes a credential.
    /// </summary>
    /// <param name="credential">The credential to remove.</param>
    /// <param name="ct">Cancellation token</param>
    public Task RemoveCredentialAsync(UserCredential credential, CancellationToken ct = default);

    /// <summary>
    /// Indicates whether a credential is owned by a specified user.
    /// </summary>
    /// <param name="user">The user to check for.</param>
    /// <param name="credential">The credential to check for.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result</returns>
    public Task<bool> IsCredentialOwnedByUser(TUser user, UserCredential credential, CancellationToken ct = default);
}
