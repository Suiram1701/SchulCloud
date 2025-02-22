using Fido2NetLib;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace SchulCloud.Identity.Enums;

/// <summary>
/// Different methods that can be used to log in into a user's account or verify 2fa with.
/// </summary>
[JsonConverter(typeof(FidoEnumConverter<LoginAttemptMethod>))]
public enum LoginAttemptMethod
{
    [EnumMember(Value = "password")]
    Password,

    [EnumMember(Value = "passkey")]
    Passkey,

    [EnumMember(Value = "twoFactor_Authenticator")]
    TwoFactorAuthenticator,

    [EnumMember(Value = "twoFactor_Email")]
    TwoFactorEmail,

    [EnumMember(Value = "twoFactor_securityKey")]
    TwoFactorSecurityKey,

    [EnumMember(Value = "twoFactor_recovery")]
    TwoFactorRecoveryCode
}
