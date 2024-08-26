using Fido2NetLib;
using Fido2NetLib.Objects;

namespace SchulCloud.Store.Options;

/// <summary>
/// Represents options that are used for credential creation and credential assertion.
/// </summary>
public class IdentityFido2Options
{
    /// <summary>
    /// Specifies whether a platform-, cross-platform-authenticator or both are allowed.
    /// </summary>
    /// <remarks>
    /// <c>null</c> indicates that both is allowed. Default set to <c>null</c>.
    /// </remarks>
    public AuthenticatorAttachment? AuthenticatorAttachment { get; set; } = null;

    /// <summary>
    /// Specifies which attestation conveyance will be requested from the authenticator.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="AttestationConveyancePreference.None"/>.
    /// </remarks>
    public AttestationConveyancePreference AttestationConveyancePreference { get; set; } = AttestationConveyancePreference.None;

    /// <summary>
    /// Specifies the user verification requirement.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="UserVerificationRequirement.Discouraged"/>.
    /// </remarks>
    public UserVerificationRequirement UserVerificationRequirement { get; set; } = UserVerificationRequirement.Discouraged;

    public AuthenticatorSelection ToAuthenticatorSelection(ResidentKeyRequirement residentKey)
    {
        return new()
        {
            AuthenticatorAttachment = AuthenticatorAttachment,
            ResidentKey = residentKey,
            UserVerification = UserVerificationRequirement
        };
    }
}