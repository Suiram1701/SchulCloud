using System.ComponentModel.DataAnnotations;

namespace SchulCloud.Server.Models;

/// <summary>
/// A model that represents sign in information to a user
/// </summary>
public class SignInModel
{
    /// <summary>
    /// The identifier of the user.
    /// </summary>
    /// <remarks>
    /// This could be the username or the email address.
    /// </remarks>
    [Required]
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the <see cref="User"/> is the email address of the user.
    /// </summary>
    /// <remarks>
    /// By default true
    /// </remarks>
    public bool IsEmailAddress { get; set; } = true;

    /// <summary>
    /// The password of the user.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the client should be remembered.
    /// </summary>
    public bool RememberMe { get; set; } = false;
}
