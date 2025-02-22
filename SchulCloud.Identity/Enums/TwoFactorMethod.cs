using Fido2NetLib;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace SchulCloud.Identity.Enums;

/// <summary>
/// Different methods for 2fa authentication.
/// </summary>
[JsonConverter(typeof(FidoEnumConverter<TwoFactorMethod>))]     // Required for JSON serialization
public enum TwoFactorMethod
{
    /// <summary>
    /// The authenticator app.
    /// </summary>
    [EnumMember(Value = "authenticator")]
    Authenticator,

    /// <summary>
    /// A code sent to the user's email.
    /// </summary>
    [EnumMember(Value = "email")]
    Email,

    /// <summary>
    /// A security key of the user.
    /// </summary>
    [EnumMember(Value = "securityKey")]
    SecurityKey,

    /// <summary>
    /// A recovery code.
    /// </summary>
    [EnumMember(Value = "recovery")]
    Recovery,
}
