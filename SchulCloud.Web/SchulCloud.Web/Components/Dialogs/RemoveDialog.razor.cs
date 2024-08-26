using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Components.Dialogs;

/// <summary>
/// A dialog for confirming the removing of an item.
/// </summary>
public sealed partial class RemoveDialog : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<RemoveDialog> Localizer { get; set; } = default!;
    #endregion

    private ConfirmDialog _dialog = default!;

    /// <summary>
    /// Shows the dialog with a title and message.
    /// </summary>
    /// <param name="title">The title to show.</param>
    /// <param name="message">The message to show.</param>
    /// <returns>Indicates whether it was confirmed or not.</returns>
    public async Task<bool> ShowAsync(string title, string message)
    {
        return await _dialog.ShowAsync(title, message, new()
        {
            NoButtonColor = ButtonColor.Secondary,
            YesButtonColor = ButtonColor.Danger,
            NoButtonText = Localizer["noBtn"],
            YesButtonText = Localizer["yesBtn"],
            AutoFocusYesButton = false
        }).ConfigureAwait(false);
    }
}
