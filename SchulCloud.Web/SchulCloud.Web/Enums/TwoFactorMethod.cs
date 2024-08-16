namespace SchulCloud.Web.Enums;

/// <summary>
/// Different methods for 2fa authentication.
/// </summary>
public enum TwoFactorMethod
{
    /// <summary>
    /// The authenticator app.
    /// </summary>
    Authenticator,

    /// <summary>
    /// A code sent to the user's email.
    /// </summary>
    Email,

    /// <summary>
    /// A recovery code.
    /// </summary>
    Recovery,
}
