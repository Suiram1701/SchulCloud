using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using SchulCloud.Web.Models;

namespace SchulCloud.Web.Components.Dialogs;

/// <summary>
/// A dialog that shows a modal to change a name.
/// </summary>
public sealed partial class RenameDialog : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<RenameDialog> Localizer { get; set; } = default!;
    #endregion

    private Modal _modal = default!;
    private string? _title = string.Empty;

    private RenameModel _model = new();
    private TaskCompletionSource<string?>? _completionSource;

    /// <summary>
    /// Shows the dialog.
    /// </summary>
    /// <param name="oldName">The old name of the item.</param>
    /// <param name="excludedNames">Names that are already token and should also be excluded.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <returns>The new name. If <c>null</c> the user cancelled the dialog.</returns>
    public async Task<string?> ShowAsync(string? oldName, IEnumerable<string?>? excludedNames = null, string? title = null)
    {
        _title = title ?? Localizer["defaultTitle"];
        _model = new()
        {
            OldName = oldName,
            NewName = oldName,
            ExcludedNames = excludedNames ?? []
        };

        await _modal.ShowAsync().ConfigureAwait(false);

        _completionSource = new();
        return await _completionSource.Task.ConfigureAwait(false);
    }

    private async Task Close_ClickAsync()
    {
        await _modal.HideAsync().ConfigureAwait(false);

        _completionSource!.SetResult(null);
        _completionSource = null;
    }

    private async Task ValidSubmitAsync()
    {
        await _modal.HideAsync().ConfigureAwait(false);

        _completionSource!.SetResult(_model!.NewName);
        _completionSource = null;
    }
}
