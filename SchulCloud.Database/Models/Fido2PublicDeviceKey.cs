using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.Models;

/// <summary>
/// A public device key owned by a fido2 credential.
/// </summary>
internal class Fido2PublicDeviceKey
{
    /// <summary>
    /// The id of the credential.
    /// </summary>
    public byte[] CredentialId { get; set; } = [];

    /// <summary>
    /// The saved key.
    /// </summary>
    public byte[] PublicDeviceKey { get; set; } = [];
}
