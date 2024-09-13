using MudBlazor;

namespace SchulCloud.Web.Components.Dialogs;

public static class DialogServiceExtensions
{
    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="service">The service to use.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message displayed by the dialog.</param>
    /// <param name="confirmColor">The color of the confirm button. <see cref="Color.Primary"/> by default.</param>
    /// <returns>A reference to the dialog. If <see cref="IDialogReference.Result"/> is <c>true</c> the user confirmed the dialog otherwise not (nullable).</returns>
    public static async Task<IDialogReference> ShowConfirmDialogAsync(this IDialogService service, string title, string? message, Color confirmColor = Color.Primary)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(title);

        DialogParameters<ConfirmDialog> parameters = new()
        {
            { dialog => dialog.Message, message },
            { dialog => dialog.ConfirmColor, confirmColor }
        };
        return await service.ShowAsync<ConfirmDialog>(title, parameters);
    }

    /// <summary>
    /// Shows a rename dialog.
    /// </summary>
    /// <param name="service">The service to use.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message displayed by the dialog.</param>
    /// <param name="oldName">The old name of the item (displayed as placeholder).</param>
    /// <param name="excludedNames">Names to exclude at show an error on input them.</param>
    /// <returns>
    /// A reference to the dialog. If the rename was successful <see cref="IDialogReference.Result"/> contains the new name.
    /// If <see cref="IDialogReference.Result"/> is cancelled the rename was cancelled.
    /// </returns>
    public static async Task<IDialogReference> ShowRenameDialogAsync(this IDialogService service, string title, string? message, string? oldName = null, IEnumerable<string>? excludedNames = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(title);

        DialogParameters<RenameDialog> parameters = new()
        {
            { dialog => dialog.Message, message },
            { dialog => dialog.OldName, oldName },
            { dialog => dialog.ExcludedNames, excludedNames }
        };
        return await service.ShowAsync<RenameDialog>(title, parameters);
    }
}
