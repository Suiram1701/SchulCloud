namespace SchulCloud.Database.Enums;

/// <summary>
/// Flags that represents different 2fa methods.
/// </summary>
[Flags]
public enum TwoFactorMethod
{
    /// <summary>
    /// General and Authenticator app.
    /// </summary>
    Authenticator = 1,

    /// <summary>
    /// Email code.
    /// </summary>
    Email = 2,
}
