namespace SchulCloud.Web.Models;

/// <summary>
/// The model for the <see cref="Components.Pages.Account.Security.ChangePassword"/> page.
/// </summary>
public class PasswordChangeModel : PasswordResetModel
{
    /// <summary>
    /// The current password of the user.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;
}