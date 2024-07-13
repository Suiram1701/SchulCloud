using System.ComponentModel.DataAnnotations;

namespace SchulCloud.Server.Models;

public class PasswordResetModel
{
    /// <summary>
    /// The name or the email of the user
    /// </summary>
    [Required]
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// The new password.
    /// </summary>
    [Required]
    public string NewPassword { get; set; } = default!;

    /// <summary>
    /// The confirmed password.
    /// </summary>
    [Required]
    public string ConfirmedPassword { get; set; } = default!;
}
