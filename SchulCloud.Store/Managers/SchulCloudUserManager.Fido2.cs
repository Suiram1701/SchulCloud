using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchulCloud.Store.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Store.Managers;

partial class SchulCloudUserManager<TUser, TCredential>
{
    /// <summary>
    /// Indicates whether the store supports fido2 credentials
    /// </summary>
    public virtual bool SupportsUserFido2Credentials
    {
        get
        {
            ThrowIfDisposed();
            return Store is IUserFido2CredentialStore<TCredential, TUser>;
        }
    }

    /// <summary>
    /// Creates options that can be used to request a fido2 credential creation.
    /// </summary>
    /// <remarks>
    /// NOTE: If <paramref name="attestationConveyance"/> is set to <see cref="AttestationConveyancePreference.None"/> the <see cref="GetFido2CredentialMetadataStatementAsync(TCredential)"/> won't be available.
    /// </remarks>
    /// <param name="user">The user that will own the created credential.</param>
    /// <param name="selection">Options for creating the credential.</param>
    /// <param name="attestationConveyance">The attestation conveyance.</param>
    /// <returns>The created options.</returns>
    public virtual async Task<CredentialCreateOptions> CreateFido2CreationOptionsAsync(TUser user, AuthenticatorSelection selection, AttestationConveyancePreference attestationConveyance = AttestationConveyancePreference.None)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        IFido2 fido2 = GetFido2Service();
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        Fido2User fido2User = await store.UserToFido2UserAsync(user, CancellationToken).ConfigureAwait(false);

        IEnumerable<TCredential> existingCreds = await store.GetCredentialsByUserAsync(user, CancellationToken).ConfigureAwait(false);
        PublicKeyCredentialDescriptor[] existingKeys = await Task.WhenAll(existingCreds.Select(cred => store.GetCredentialDescriptorAsync(cred, CancellationToken))).ConfigureAwait(false);

        AuthenticationExtensionsClientInputs extensions = new()
        {
            Extensions = true,
            UserVerificationMethod = true,
            CredProps = true
        };
        return fido2.RequestNewCredential(fido2User, [..existingKeys], selection, attestationConveyance, extensions);
    }

    /// <summary>
    /// Stores a created credential.
    /// </summary>
    /// <param name="user">The user that should own the credential.</param>
    /// <param name="securityKeyName">A user specified name of the security key.</param>
    /// <param name="usernamelessAllowed">Indicates whether usernameless sign ins are allowed with the credential.</param>
    /// <param name="authenticatorResponse">The raw response of the authenticator.</param>
    /// <param name="options">The options that were used to request the credential creation from the authenticator.</param>
    /// <returns>The result of the operation.</returns>
    public virtual async Task<IdentityResult> StoreFido2CredentialsAsync(TUser user, string? securityKeyName, bool usernamelessAllowed, AuthenticatorAttestationRawResponse authenticatorResponse, CredentialCreateOptions options)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(authenticatorResponse);
        ArgumentNullException.ThrowIfNull(options);

        IFido2 fido2 = GetFido2Service();
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        async Task<bool> isUniqueCallback(IsCredentialIdUniqueToUserParams @params, CancellationToken ct)
        {
            return await store.GetCredentialById(@params.CredentialId, ct).ConfigureAwait(false) is null;
        }

        try
        {
            // NOTE: Currently every verification error throws an exception.
            MakeNewCredentialResult makeResult = await fido2.MakeNewCredentialAsync(authenticatorResponse, options, isUniqueCallback, CancellationToken).ConfigureAwait(false);
            if (makeResult.Result is null)
            {
                throw new Fido2VerificationException(makeResult.ErrorMessage);
            }

            await store.CreateCredentialAsync(user, securityKeyName, usernamelessAllowed, makeResult.Result, CancellationToken).ConfigureAwait(false);
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
    /// <param name="verificationRequirement">The verification requirement.</param>
    /// <returns>The created options.</returns>
    public virtual async Task<AssertionOptions> CreateFido2AssertionOptionsAsync(TUser? user, UserVerificationRequirement verificationRequirement = UserVerificationRequirement.Preferred)
    {
        ThrowIfDisposed();
        IFido2 fido2 = GetFido2Service();
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        PublicKeyCredentialDescriptor[] existingKeys = [];
        if (user is not null)
        {
            IEnumerable<TCredential> existingCreds = await store.GetCredentialsByUserAsync(user, CancellationToken).ConfigureAwait(false);
            existingKeys = await Task.WhenAll(existingCreds.Select(cred => store.GetCredentialDescriptorAsync(cred, CancellationToken))).ConfigureAwait(false);
        }

        AuthenticationExtensionsClientInputs extensions = new()
        {
            Extensions = true,
            UserVerificationMethod = true,
            DevicePubKey = new()
        };

        return fido2.GetAssertionOptions([.. existingKeys], verificationRequirement, extensions);
    }

    /// <summary>
    /// Makes an fido2 assertion of the <paramref name="authenticatorResponse"/>.
    /// </summary>
    /// <remarks>
    /// NOTE: <paramref name="user"/> and <paramref name="options"/> have to be the same as used in <see cref="CreateFido2CreationOptionsAsync(TUser, AuthenticatorSelection, AttestationConveyancePreference)"/>.
    /// </remarks>
    /// <param name="user">The user that requested the assertion. If <c>null</c> the assertion was requested as usernameless.</param>
    /// <param name="authenticatorResponse">The raw response of the authenticator.</param>
    /// <param name="options">The options that were used to request the assertion from the authenticator.</param>
    /// <returns>
    /// If <paramref name="user"/> was specified <c>user</c> will be the same instance otherwise it will be the from the credential determined user.
    /// <c>credential</c> is the credential that were used by the authenticator.
    /// If <c>null</c> the assertion wasn't successful or the <paramref name="authenticatorResponse"/> isn't valid.
    /// </returns>
    public virtual async Task<(TUser user, TCredential credential)?> MakeFido2AssertionAsync(TUser? user, AuthenticatorAssertionRawResponse authenticatorResponse, AssertionOptions options)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(authenticatorResponse);
        ArgumentNullException.ThrowIfNull(options);

        IFido2 fido2 = GetFido2Service();
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        if (await store.GetCredentialById(authenticatorResponse.Id, CancellationToken).ConfigureAwait(false) is not TCredential credential)
        {
            return null;
        }

        if (user is null && await store.GetCredentialUsernamelessAllowedAsync(credential, CancellationToken).ConfigureAwait(false) is false)
        {
            return null;
        }
        user ??= await store.GetCredentialOwnerAsync(credential, CancellationToken).ConfigureAwait(false);

        async Task<bool> isOwnedByUserHandleCallback(IsUserHandleOwnerOfCredentialIdParams @params, CancellationToken ct)
        {
            return await store.IsCredentialOwnedByUserHandle(@params.CredentialId, @params.UserHandle, CancellationToken).ConfigureAwait(false);
        }

        uint signCount = await store.GetCredentialSignCountAsync(credential, CancellationToken).ConfigureAwait(false);
        byte[] publicKey = await store.GetCredentialPublicKeyAsync(credential, CancellationToken).ConfigureAwait(false);
        IEnumerable<byte[]> publicDeviceKeys = await store.GetCredentialPublicDeviceKeys(credential, CancellationToken).ConfigureAwait(false);

        try
        {
            // NOTE: Every verification error throws an exception.
            VerifyAssertionResult result = await fido2.MakeAssertionAsync(authenticatorResponse, options, publicKey, [..publicDeviceKeys], signCount, isOwnedByUserHandleCallback, CancellationToken).ConfigureAwait(false);

            await store.SetCredentialSignCountAsync(credential, result.SignCount, CancellationToken).ConfigureAwait(false);
            if (result.DevicePublicKey is not null)
            {
                await store.AddCredentialPublicDeviceKey(credential, result.DevicePublicKey, CancellationToken).ConfigureAwait(false);
            }
        }
        catch (Fido2VerificationException ex)
        {
            LogFido2AssertionError(Logger, ex.Message);
            return null;
        }

        IdentityResult updateResult = await UpdateAsync(user).ConfigureAwait(false);
        return updateResult.Succeeded
            ? (user, credential)
            : null;
    }

    /// <summary>
    /// Gets the fido2 credentials of a user.
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns>The credentials</returns>
    public virtual async Task<IEnumerable<TCredential>> GetFido2CredentialsByUserAsync(TUser user)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        return await store.GetCredentialsByUserAsync(user, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the security key name of a credential (specified by the user).
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <returns>The name of the key.</returns>
    public virtual async Task<string?> GetFido2CredentialSecurityKeyNameAsync(TCredential credential)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        return await store.GetCredentialSecurityKeyNameAsync(credential, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the transports flags of a credential. These flags represents information about the used authenticator.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <returns>The flags. If <c>null</c> the authenticator haven't provided these flags.</returns>
    public virtual async Task<AuthenticatorTransport[]?> GetFido2CredentialTransportsAsync(TCredential credential)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        PublicKeyCredentialDescriptor descriptor = await store.GetCredentialDescriptorAsync(credential, CancellationToken).ConfigureAwait(false);
        return descriptor.Transports;
    }

    /// <summary>
    /// Gets the utc registration datetime of a credential.
    /// </summary>
    /// <param name="credential">The credential.</param>
    /// <returns>The datetime.</returns>
    public virtual async Task<DateTime> GetFido2CredentialRegistrationDateAsync(TCredential credential)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        return await store.GetCredentialRegistrationDateAsync(credential, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the metadata statement of the authenticator used for a credential.
    /// </summary>
    /// <remarks>
    /// NOTE: To provide these information, <c>attestationConveyance</c> mustn't be <see cref="AttestationConveyancePreference.None"/> on registration.
    /// </remarks>
    /// <param name="credential">The credential.</param>
    /// <returns>The metadata statement. If <c>null</c> the authenticator haven't provided these information on registration.</returns>
    public virtual async Task<MetadataStatement?> GetFido2CredentialMetadataStatementAsync(TCredential credential)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        IMetadataService metadataService = GetMetadataService();
        IUserFido2CredentialStore<TCredential, TUser> store = GetFido2CredentialStore();

        Guid aaguid = await store.GetCredentialAaGuidAsync(credential, CancellationToken).ConfigureAwait(false);
        if (aaguid.Equals(Guid.Empty))
        {
            return null;
        }
        MetadataBLOBPayloadEntry? entry = await metadataService.GetEntryAsync(aaguid, CancellationToken).ConfigureAwait(false);
        return entry?.MetadataStatement;
    }

    private IUserFido2CredentialStore<TCredential, TUser> GetFido2CredentialStore()
    {
        if (Store is not IUserFido2CredentialStore<TCredential, TUser> cast)
        {
            throw new NotSupportedException($"{nameof(IUserFido2CredentialStore<TCredential, TUser>)} isn't supported by the store.");
        }
        return cast;
    }

    private IFido2 GetFido2Service() => _services.GetRequiredService<IFido2>();

    private IMetadataService GetMetadataService() => _services.GetRequiredService<IMetadataService>();

    [LoggerMessage(LogLevel.Debug, "An error occurred on fido2 assertion: {message}")]
    public static partial void LogFido2AssertionError(ILogger logger, string message);

    [LoggerMessage(LogLevel.Debug, "An error occurred on fido2 attestation: {message}")]
    public static partial void LogFido2AttestationError(ILogger logger, string message);
}
