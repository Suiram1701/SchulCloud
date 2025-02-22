using Fido2NetLib;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace SchulCloud.Identity.Enums;

/// <summary>
/// The result of a login attempt.
/// </summary>
[JsonConverter(typeof(FidoEnumConverter<LoginAttemptResult>))]
public enum LoginAttemptResult
{
    /// <summary>
    /// Indicates that the attempt succeeded.
    /// </summary>
    [EnumMember(Value = "succeeded")]
    Succeeded,

    /// <summary>
    /// The attempt failed. For example by a wrong password.
    /// </summary>
    [EnumMember(Value = "failed")]
    Failed,

    /// <summary>
    /// A second factor was required to continue log in.
    /// </summary>
    [EnumMember(Value = "twoFactorRequired")]
    TwoFactorRequired,

    /// <summary>
    /// The account was locked. Reasons could be that an admin locked the account or too many failed attempts.
    /// </summary>
    [EnumMember(Value = "lockedOut")]
    LockedOut,

    /// <summary>
    /// It was not allowed at the time to log in into that account.
    /// </summary>
    [EnumMember(Value = "notAllowed")]
    NotAllowed
}
