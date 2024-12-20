namespace SchulCloud.Database.Enums;

/// <summary>
/// Different methods that can be used to log in into a user's account or verify 2fa with.
/// </summary>
internal enum LoginAttemptMethod
{
    Password,
    Passkey,
    TwoFactorAuthenticator,
    TwoFactorEmail,
    TwoFactorSecurityKey,
    TwoFactorRecoveryCode
}
