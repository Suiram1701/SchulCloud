using SchulCloud.Web.Enums;

namespace SchulCloud.Web.Models;

public class Verify2faModel : AuthenticatorModel
{
    /// <summary>
    /// Indicates whether the client should be remembered for further 2fa logins.
    /// </summary>
    public bool RememberClient { get; set; }

    /// <summary>
    /// The 2fa method the <see cref="AuthenticatorModel.Code"/> is used for.
    /// </summary>
    public TwoFactorMethod Method { get; set; } = TwoFactorMethod.Authenticator;
}
