using Fido2NetLib.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.Models;

/// <summary>
/// A FIDO2 credential owned by a user.
/// </summary>
public class Fido2Credential
{
    /// <summary>
    /// The id of the credential.
    /// </summary>
    public byte[] Id { get; set; } = [];

    /// <summary>
    /// The id of the credential owner.
    /// </summary>
    public string UserId { get; set; } = default!;

    /// <summary>
    /// The displayed name of the key.
    /// </summary>
    public string? SecurityKeyName { get; set; }

    /// <summary>
    /// Indicates whether this security key is allowed to perform a usernameless sign in.
    /// </summary>
    public bool IsUsernameless { get; set; }

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

    internal ICollection<Fido2PublicDeviceKey> DevicePublicKeys { get; set; } = [];

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
}
