namespace SchulCloud.Database.Enums;

/// <summary>
/// Flags that represents different 2fa methods.
/// </summary>
[Flags]
public enum TwoFactorMethod
{
    Authenticator = 1,
    Email = 2,
    SecurityKey = 4
}
