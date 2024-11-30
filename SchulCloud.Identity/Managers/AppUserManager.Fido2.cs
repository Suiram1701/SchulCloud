using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchulCloud.Identity.Abstractions;
using SchulCloud.Identity.Models;
using SchulCloud.Identity.Options;
using System.Text;

namespace SchulCloud.Store.Managers;

partial class AppUserManager<TUser>
{
    /// <summary>
    /// Indicates whether the store supports fido2 credentials
    /// </summary>
    public virtual bool SupportsUserCredentials
    {
        get
        {
            ThrowIfDisposed();
            return Store is IUserCredentialStore<TUser> && _services.GetService<IFido2>() is not null;
        }
    }

    /// <summary>
    /// Creates options that can be used to request a fido2 credential creation.
    /// </summary>
    /// <param name="user">The user that will own the created credential.</param>
    /// <param name="isPasskey">Indicates whether the </param>
    /// <returns>The created options.</returns>
    public virtual async Task<CredentialCreateOptions> CreateFido2CreationOptionsAsync(TUser user, bool isPasskey)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        IFido2 fido2 = GetFido2Service();
        IUserCredentialStore<TUser> store = GetCredentialStore();

        Fido2User fido2User = new()
        {
            Id = Encoding.UTF8.GetBytes(await GetUserIdAsync(user)),
            Name = await GetEmailAsync(user),
            DisplayName = await GetUserNameAsync(user)
        };

        IEnumerable<UserCredential> existingCreds = await store.FindCredentialsByUserAsync(user, CancellationToken).ConfigureAwait(false);
        PublicKeyCredentialDescriptor[] existingKeys = existingCreds.Select(cred => cred.ToCredentialDescriptor()).ToArray();

        ResidentKeyRequirement residentKey = isPasskey
            ? ResidentKeyRequirement.Required
            : ResidentKeyRequirement.Discouraged;
        IdentityFido2Options options = GetFido2Options();

        AuthenticationExtensionsClientInputs extensions = new()
        {
            Extensions = true,
            UserVerificationMethod = true,
            CredProps = true
        };
        return fido2.RequestNewCredential(fido2User, [.. existingKeys], options.ToAuthenticatorSelection(residentKey), options.AttestationConveyancePreference, extensions);
    }

    /// <summary>
    /// Stores a created credential.
    /// </summary>
    /// <param name="user">The user that should own the credential.</param>
    /// <param name="name">A user specified name of the credential.</param>
    /// <param name="isPasskey">Indicates The credential is a passkey.</param>
    /// <param name="authenticatorResponse">The raw response of the authenticator.</param>
    /// <param name="options">The options that were used to request the credential creation from the authenticator.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> StoreFido2CredentialsAsync(TUser user, string? name, bool isPasskey, AuthenticatorAttestationRawResponse authenticatorResponse, CredentialCreateOptions options)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(authenticatorResponse);
        ArgumentNullException.ThrowIfNull(options);

        IFido2 fido2 = GetFido2Service();
        IUserCredentialStore<TUser> store = GetCredentialStore();

        async Task<bool> isUniqueCallback(IsCredentialIdUniqueToUserParams @params, CancellationToken ct)
        {
            return await store.FindCredentialAsync(@params.CredentialId, ct).ConfigureAwait(false) is null;
        }

        try
        {
            // NOTE: Currently every verification error throws an exception.
            MakeNewCredentialResult makeResult = await fido2.MakeNewCredentialAsync(authenticatorResponse, options, isUniqueCallback, CancellationToken).ConfigureAwait(false);
            if (makeResult.Result is null)
            {
                throw new Fido2VerificationException(makeResult.ErrorMessage);
            }

            UserCredential credential = new()
            {
                Id = makeResult.Result.Id,
                Name = name,
                PublicKey = makeResult.Result.PublicKey,
                SignCount = makeResult.Result.SignCount,
                Transports = makeResult.Result.Transports,
                IsBackupEligible = makeResult.Result.IsBackupEligible,
                IsBackedUp = makeResult.Result.IsBackedUp,
                AttestationObject = makeResult.Result.AttestationObject,
                AttestationClientDataJson = makeResult.Result.AttestationClientDataJson,
                AttestationFormat = makeResult.Result.AttestationFormat,
                RegDate = DateTime.UtcNow,
                AaGuid = makeResult.Result.AaGuid
            };
            await store.AddCredentialAsync(user, credential, CancellationToken).ConfigureAwait(false);

            if (isPasskey && SupportsUserPasskeys)
            {
                IUserPasskeysStore<TUser> passkeysStore = GetPasskeysStore();
                await passkeysStore.SetIsPasskeyCredentialAsync(credential, true, CancellationToken).ConfigureAwait(false);
            }

            return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
        }
        catch (Fido2VerificationException ex)
        {
            IdentityError error = new()
            {
                Code = "MakeCredentialFailed",
                Description = ex.Message ?? "An error occurred while parsing the fido2 credential."
            };
            return IdentityResult.Failed(error);
        }
    }

    /// <summary>
    /// Creates options for a fido2 assertion.
    /// </summary>
    /// <param name="user">The user that requested the assertion. If <c>null</c> the assertion will be done as usernameless.</param>
    /// <returns>The created options.</returns>
    public virtual async Task<AssertionOptions> CreateFido2AssertionOptionsAsync(TUser? user)
    {
        ThrowIfDisposed();
        IFido2 fido2 = GetFido2Service();
        IUserCredentialStore<TUser> store = GetCredentialStore();

        PublicKeyCredentialDescriptor[] existingKeys = [];
        if (user is not null)
        {
            IEnumerable<UserCredential> existingCreds = await store.FindCredentialsByUserAsync(user, CancellationToken).ConfigureAwait(false);
            existingKeys = existingCreds.Select(cred => cred.ToCredentialDescriptor()).ToArray();
        }

        AuthenticationExtensionsClientInputs extensions = new()
        {
            Extensions = true,
            UserVerificationMethod = true
        };
        return fido2.GetAssertionOptions([.. existingKeys], GetFido2Options().UserVerificationRequirement, extensions);
    }

    /// <summary>
    /// Makes an fido2 assertion of the <paramref name="authenticatorResponse"/>.
    /// </summary>
    /// <remarks>
    /// NOTE: <paramref name="user"/> and <paramref name="options"/> have to be the same as used in <see cref="CreateFido2AssertionOptionsAsync(TUser?)"/>.
    /// </remarks>
    /// <param name="user">The user that requested the assertion. If <c>null</c> the assertion was requested as usernameless.</param>
    /// <param name="authenticatorResponse">The raw response of the authenticator.</param>
    /// <param name="options">The options that were used to request the assertion from the authenticator.</param>
    /// <returns>
    /// If <paramref name="user"/> was specified <c>user</c> will be the same instance otherwise it will be the from the credential determined user.
    /// <c>credential</c> is the credential that were used by the authenticator.
    /// The credential used for the assertion. If <c>null</c> the assertion wasn't successful or the <paramref name="authenticatorResponse"/> isn't valid.
    /// </returns>
    public virtual async Task<UserCredential?> MakeFido2AssertionAsync(TUser? user, AuthenticatorAssertionRawResponse authenticatorResponse, AssertionOptions options)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(authenticatorResponse);
        ArgumentNullException.ThrowIfNull(options);

        IFido2 fido2 = GetFido2Service();
        IUserCredentialStore<TUser> store = GetCredentialStore();

        if (await store.FindCredentialAsync(authenticatorResponse.Id, CancellationToken).ConfigureAwait(false) is not UserCredential credential)
        {
            return null;
        }

        async Task<bool> isOwnedByUserHandleCallback(IsUserHandleOwnerOfCredentialIdParams @params, CancellationToken ct)
        {
            TUser? user = await FindByIdAsync(Encoding.UTF8.GetString(@params.UserHandle));
            UserCredential? credential = await store.FindCredentialAsync(@params.CredentialId, ct);

            return user is not null && credential is not null && await store.IsCredentialOwnedByUser(user, credential, ct);
        }

        try
        {
            // NOTE: Every verification error throws an exception.
            VerifyAssertionResult result = await fido2.MakeAssertionAsync(
                authenticatorResponse,
                options,
                credential.PublicKey,
                [],
                credential.SignCount,
                isOwnedByUserHandleCallback,
                CancellationToken).ConfigureAwait(false);

            credential.SignCount = result.SignCount;
            credential.IsBackedUp = result.IsBackedUp;
            await store.UpdateCredentialAsync(credential, CancellationToken);
        }
        catch (Fido2VerificationException ex)
        {
            LogFido2AssertionError(Logger, ex.Message);
            return null;
        }

        user ??= await store.FindUserByCredentialAsync(credential, CancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            LogFido2AssertionError(Logger, "Owner of credential not found.");
            return null;
        }

        IdentityResult updateResult = await UpdateAsync(user).ConfigureAwait(false);
        return updateResult.Succeeded
            ? credential
            : null;
    }

    /// <summary>
    /// Gets a fido2 credential by its id.
    /// </summary>
    /// <param name="credId">The id of the credential.</param>
    /// <returns>The credential if found.</returns>
    public virtual async Task<UserCredential?> FindFido2Credential(byte[] credId)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credId);
        IUserCredentialStore<TUser> store = GetCredentialStore();

        return await store.FindCredentialAsync(credId, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds every fido2 credentials that a user owns.
    /// </summary>
    /// <param name="user">The user to find the credentials of.</param>
    /// <returns>The credentials</returns>
    public virtual async Task<IEnumerable<UserCredential>> FindFido2CredentialsByUserAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        IUserCredentialStore<TUser> store = GetCredentialStore();
        return await store.FindCredentialsByUserAsync(user, CancellationToken);
    }

    /// <summary>
    /// Finds a user by one of the credentials he owns.
    /// </summary>
    /// <param name="credential">The credential to find the owner for.</param>
    /// <returns>The credential owner.</returns>
    public virtual async Task<TUser?> FindUserByFido2CredentialAsync(UserCredential credential)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);

        IUserCredentialStore<TUser> store = GetCredentialStore();
        return await store.FindUserByCredentialAsync(credential, CancellationToken);
    }

    /// <summary>
    /// Updates the name of a fido2 credential.
    /// </summary>
    /// <param name="user">The user that owns this credential.</param>
    /// <param name="credential">The credential to update the name of.</param>
    /// <param name="name">The new name of the credential.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> UpdateFido2CredentialNameAsync(TUser user, UserCredential credential, string name)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        IUserCredentialStore<TUser> store = GetCredentialStore();

        credential.Name = name;
        await store.UpdateCredentialAsync(credential, CancellationToken).ConfigureAwait(false);

        return await UpdateAsync(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a fido2 credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <param name="user">The user that owns the credential.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> RemoveFido2CredentialAsync(UserCredential credential, TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);

        IUserCredentialStore<TUser> store = GetCredentialStore();
        await store.RemoveCredentialAsync(credential, CancellationToken).ConfigureAwait(false);

        return await UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the metadata statement of the authenticator used for a credential.
    /// </summary>
    /// <remarks>
    /// NOTE: To provide these information, <c>attestationConveyance</c> mustn't be <see cref="AttestationConveyancePreference.None"/> on registration.
    /// </remarks>
    /// <param name="credential">The credential.</param>
    /// <returns>The metadata statement. If <c>null</c> the authenticator haven't provided these information on registration.</returns>
    public virtual async Task<MetadataStatement?> GetFido2CredentialMetadataStatementAsync(UserCredential credential)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        IMetadataService metadataService = GetMetadataService();

        if (credential.AaGuid.Equals(Guid.Empty))
        {
            return null;
        }
        MetadataBLOBPayloadEntry? entry = await metadataService.GetEntryAsync(credential.AaGuid, CancellationToken).ConfigureAwait(false);
        return entry?.MetadataStatement;
    }

    private IUserCredentialStore<TUser> GetCredentialStore()
    {
        if (Store is not IUserCredentialStore<TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserCredentialStore<TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IFido2 GetFido2Service() => _services.GetRequiredService<IFido2>();

    private IdentityFido2Options GetFido2Options() => _services.GetRequiredService<IOptions<IdentityFido2Options>>().Value;

    private IMetadataService GetMetadataService() => _services.GetRequiredService<IMetadataService>();

    [LoggerMessage(LogLevel.Debug, "An error occurred on fido2 assertion: {message}")]
    private static partial void LogFido2AssertionError(ILogger logger, string message);

    [LoggerMessage(LogLevel.Debug, "An error occurred on fido2 attestation: {message}")]
    private static partial void LogFido2AttestationError(ILogger logger, string message);
}
