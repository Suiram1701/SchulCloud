using SchulCloud.Identity.Enums;

namespace SchulCloud.RestApi.Models;

/// <summary>
/// The security settings of user.
/// </summary>
/// <param name="HasPassword">Indicates whether the user has set a password. If <c>null</c> password aren't internally supported.</param>
/// <param name="TwoFactorEnabled">Indicates whether the users has 2fa enabled. If <c>null</c> 2fa isn't internally supported.</param>
/// <param name="Enabled2faMethods">A collection of the enabled 2fa verification methods if <see cref="TwoFactorEnabled"/> is <c>true</c>. If <c>null</c> 2fa isn't internally supported.</param>
/// <param name="PasskeysEnabled">Indicates whether the user has username less sign in (passkey sign in) using security keys is enabled. If <c>null</c> security keys aren't internally supported.</param>
/// <param name="Fido2Credentials">A collection of registered security keys by the user. If <c>null</c> security keys aren't internally supported.</param>
public record SecuritySettings(
    bool? HasPassword,
    bool? TwoFactorEnabled,
    TwoFactorMethod[]? Enabled2faMethods,
    bool? PasskeysEnabled,
    Fido2Credential[]? Fido2Credentials);