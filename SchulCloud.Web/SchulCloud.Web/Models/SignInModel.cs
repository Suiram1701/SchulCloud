using System.ComponentModel.DataAnnotations;

namespace SchulCloud.Web.Models;

/// <summary>
/// A model that represents sign in information to a user
/// </summary>
public class SignInModel
{
    /// <summary>
    /// A key to access auth data stored on the server.
    /// </summary>
    public string? AuthenticatorDataAccessKey { get; set; }

    /// <summary>
    /// The identifier of the user.
    /// </summary>
    /// <remarks>
    /// This could be the username or the email address.
    /// </remarks>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// The password of the user.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the client should be remembered.
    /// </summary>
    public bool Persistent { get; set; } = false;
}
