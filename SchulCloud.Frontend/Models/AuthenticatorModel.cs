namespace SchulCloud.Frontend.Models;

/// <summary>
/// A model that contains a user authenticator code.
/// </summary>
public class AuthenticatorModel
{
    /// <summary>
    /// The by the user entered code.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// The trimmed code.
    /// </summary>
    public string TrimmedCode => Code?.Replace(" ", "") ?? string.Empty;
}
