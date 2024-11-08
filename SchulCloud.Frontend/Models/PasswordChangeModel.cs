namespace SchulCloud.Frontend.Models;

/// <summary>
/// The model for the <see cref="Components.Pages.Account.Security.ChangePassword"/> page.
/// </summary>
public class PasswordChangeModel
{
    /// <summary>
    /// The current password of the user.
    /// </summary>
    public string CurrentPassword { get; set; } = default!;

    /// <summary>
    /// The new password.
    /// </summary>
    public string NewPassword { get; set; } = default!;

    /// <summary>
    /// The <see cref="NewPassword"/> again as confirmation.
    /// </summary>
    public string ConfirmedPassword { get; set; } = default!;
}