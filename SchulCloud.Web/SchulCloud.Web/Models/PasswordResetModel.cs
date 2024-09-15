namespace SchulCloud.Web.Models;

/// <summary>
/// The model for the <see cref="Components.Pages.Auth.ResetPassword"/> page.
/// </summary>
public class PasswordResetModel
{
    /// <summary>
    /// The name or the email of the user.
    /// </summary>
    public string User { get; set; } = default!;

    /// <summary>
    /// The new password.
    /// </summary>
    public string NewPassword { get; set; } = default!;

    /// <summary>
    /// The <see cref="NewPassword"/> again as confirmation.
    /// </summary>
    public string ConfirmedPassword { get; set; } = default!;
}