namespace SchulCloud.Web.Models;

/// <summary>
/// A model that contains a user authenticator code.
/// </summary>
public class AuthenticatorModel
{
    /// <summary>
    /// The by the user entered code.
    /// </summary>
    public string? Code { get; set; }
}
