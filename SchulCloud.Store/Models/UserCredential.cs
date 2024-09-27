using Fido2NetLib.Objects;

namespace SchulCloud.Store.Models;

/// <summary>
/// A credential owned by a user.
/// </summary>
public class UserCredential
{
    /// <summary>
    /// The id of the credential.
    /// </summary>
    public byte[] Id { get; set; } = [];

    /// <summary>
    /// The displayed name of the key.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The public key of the credential.
    /// </summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>
    /// The count of created signatures.
    /// </summary>
    public uint SignCount { get; set; }

    /// <summary>
    /// Contains flags that represents information about the used security key.
    /// </summary>
    public AuthenticatorTransport[]? Transports { get; set; }

    public bool IsBackupEligible { get; set; }

    public bool IsBackedUp { get; set; }

    public byte[] AttestationObject { get; set; } = [];

    public byte[] AttestationClientDataJson { get; set; } = [];

    /// <summary>
    /// The format of the attestation.
    /// </summary>
    public string AttestationFormat { get; set; } = default!;

    /// <summary>
    /// The datetime where the key was registered.
    /// </summary>
    public DateTime RegDate { get; set; }

    /// <summary>
    /// The AaGuid (authenticator attestion guid) of the key.
    /// </summary>
    public Guid AaGuid { get; set; }

    public PublicKeyCredentialDescriptor ToCredentialDescriptor()
    {
        return new(PublicKeyCredentialType.PublicKey, Id, Transports);
    }
}
