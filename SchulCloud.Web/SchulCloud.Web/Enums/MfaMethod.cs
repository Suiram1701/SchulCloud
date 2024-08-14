namespace SchulCloud.Web.Enums;

/// <summary>
/// Different methods for 2fa authentication.
/// </summary>
public enum MfaMethod
{
    /// <summary>
    /// The authenticator app.
    /// </summary>
    Authenticator,

    /// <summary>
    /// A recovery code.
    /// </summary>
    Recovery
}
