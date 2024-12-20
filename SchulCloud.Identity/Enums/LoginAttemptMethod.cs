namespace SchulCloud.Identity.Enums;

/// <summary>
/// Different methods that can be used to log in into a user's account or verify 2fa with.
/// </summary>
public enum LoginAttemptMethod
{
    Password,
    Passkey,
    TwoFactorAuthenticator,
    TwoFactorEmail,
    TwoFactorSecurityKey,
    TwoFactorRecoveryCode
}
