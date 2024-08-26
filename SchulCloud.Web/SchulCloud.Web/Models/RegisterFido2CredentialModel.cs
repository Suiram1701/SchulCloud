﻿using FluentValidation;

namespace SchulCloud.Web.Models;

public class RegisterFido2CredentialModel
{
    /// <summary>
    /// The name of the credential.
    /// </summary>
    public string SecurityKeyName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the credential can used as a passkey.
    /// </summary>
    public bool IsPasskey { get; set; }
}
