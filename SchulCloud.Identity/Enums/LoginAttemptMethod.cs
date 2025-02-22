namespace SchulCloud.Identity.Enums;

/// <summary>
/// Different methods that can be used to log in into a user's account or verify 2fa with.
/// </summary>
public enum LoginAttemptMethod
{
    /// <summary>
    /// The user's password were used for sign in.
    /// </summary>
    Password,

    /// <summary>
    /// A username less login were performed using a passkey.
    /// </summary>
    Passkey,

    /// <summary>
    /// A 2fa verification using an authenticator app were done.
    /// </summary>
    TwoFactorAuthenticator,

    /// <summary>
    /// A 2fa verification using an email were done.
    /// </summary>
    TwoFactorEmail,

    /// <summary>
    /// A 2fa verification using a security hardware key were done.
    /// </summary>
    TwoFactorSecurityKey,

    /// <summary>
    /// A 2fa verification using one of the user's recovery codes were done.
    /// </summary>
    TwoFactorRecoveryCode
}
