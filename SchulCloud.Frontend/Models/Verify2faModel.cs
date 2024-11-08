using SchulCloud.Frontend.Enums;

namespace SchulCloud.Frontend.Models;

public class Verify2faModel : AuthenticatorModel
{
    /// <summary>
    /// A key to access auth data stored on the server.
    /// </summary>
    public string? AuthenticatorDataAccessKey { get; set; }

    /// <summary>
    /// A value that is internally used by the html checkbox to store the result of <see cref="ShouldRememberClient"/>.
    /// </summary>
    public string? RememberClient { get; set; }

    /// <summary>
    /// Indicates whether the client should be remembered for further 2fa logins.
    /// </summary>
    public bool ShouldRememberClient => RememberClient == "on";

    /// <summary>
    /// The 2fa method the <see cref="AuthenticatorModel.Code"/> is used for.
    /// </summary>
    public TwoFactorMethod Method { get; set; } = TwoFactorMethod.Authenticator;
}
