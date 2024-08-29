using SchulCloud.Web.Enums;

namespace SchulCloud.Web.Models;

public class Verify2faModel : AuthenticatorModel
{
    /// <summary>
    /// A key to access auth data stored on the server.
    /// </summary>
    public string? AuthenticatorDataAccessKey { get; set; }

    /// <summary>
    /// Indicates whether the client should be remembered for further 2fa logins.
    /// </summary>
    public bool RememberClient { get; set; }

    /// <summary>
    /// The 2fa method the <see cref="AuthenticatorModel.Code"/> is used for.
    /// </summary>
    public TwoFactorMethod Method { get; set; } = TwoFactorMethod.Authenticator;
}
