using Fido2NetLib;
using Fido2NetLib.Objects;

namespace SchulCloud.Store.Abstractions;

/// <summary>
/// A store interface that provides fido2 credentials for users.
/// </summary>
/// <typeparam name="TCredential">The type of the used credential.</typeparam>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IUserFido2CredentialStore<TCredential, TUser>
    where TCredential : class
    where TUser : class
{
    /// <summary>
    /// A additional method to make an user to a fido2 user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The fido2 user.</returns>
    public Task<Fido2User> UserToFido2UserAsync(TUser user, CancellationToken ct);

    /// <summary>
    /// Gets the credential that has the id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The credential. <c>null</c> if not credential with the id was found.</returns>
    public Task<TCredential?> GetCredentialById(byte[] id, CancellationToken ct);

    /// <summary>
    /// Gets the credentials that the specified user owns.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The credentials.</returns>
    public Task<IEnumerable<TCredential>> GetCredentialsByUserAsync(TUser user, CancellationToken ct);

    /// <summary>
    /// Gets the <see cref="PublicKeyCredentialDescriptor"/> of a credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The descriptor.</returns>
    public Task<PublicKeyCredentialDescriptor> GetCredentialDescriptorAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Gets the user that owns a credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user.</returns>
    public Task<TUser> GetCredentialOwnerAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Gets the security key name of a credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The name of the security key.</returns>
    public Task<string?> GetCredentialSecurityKeyNameAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Sets the security key name of the credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="newName">The new name of the key.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetCredentialSecurityKeyNameAsync(TCredential credential, string? newName, CancellationToken ct);

    /// <summary>
    /// Gets a flag that indicates whether the credential were registered as a passkey.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The flag.</returns>
    public Task<bool> GetCredentialIsPasskeyAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Gets the public key of a credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The public key.</returns>
    public Task<byte[]> GetCredentialPublicKeyAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Gets the signature count of a credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The signature counter.</returns>
    public Task<uint> GetCredentialSignCountAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Sets the signature counter of a credential.
    /// </summary>
    /// <param name="credential">The credential to modify.</param>
    /// <param name="count">The count to set.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetCredentialSignCountAsync(TCredential credential, uint count, CancellationToken ct);

    /// <summary>
    /// Gets the registration date of a credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The registration date.</returns>
    public Task<DateTime> GetCredentialRegistrationDateAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Gets the authenticator attestation guid (aaguid) of a credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The aaguid</returns>
    public Task<Guid> GetCredentialAaGuidAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Creates a credential.
    /// </summary>
    /// <param name="user">The user that owns the credential.</param>
    /// <param name="securityKeyName">The displayed name of the credential.</param>
    /// <param name="isPasskey">Indicates whether the credential is registered as a passkey.</param>
    /// <param name="credential">The credential data.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The credential instance.</returns>
    public Task<TCredential> CreateCredentialAsync(TUser user, string? securityKeyName, bool isPasskey, RegisteredPublicKeyCredential credential, CancellationToken ct);

    /// <summary>
    /// Removes a credential.
    /// </summary>
    /// <param name="credential">The credential to delete.</param>
    /// <param name="ct">Cancellation token</param>
    public Task DeleteCredentialAsync(TCredential credential, CancellationToken ct);

    /// <summary>
    /// Indicates whether a credential with the <paramref name="credId"/> is owned by a user with the <paramref name="userHandle"/>.
    /// </summary>
    /// <param name="credId">The id of the credential.</param>
    /// <param name="userHandle">The user handle.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result.</returns>
    public Task<bool> IsCredentialOwnedByUserHandle(byte[] credId, byte[] userHandle, CancellationToken ct);
}
