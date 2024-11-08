using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace SchulCloud.Frontend.Components.Dialogs;

public sealed partial class ConfirmDialog
{
    #region Injections
    [Inject]
    private IStringLocalizer<ConfirmDialog> Localizer { get; set; } = default!;
    #endregion

    [CascadingParameter]
    private MudDialogInstance DialogInstance { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = default!;

    [Parameter]
    public Color ConfirmColor { get; set; } = Color.Primary;

    private void Cancel_Click() => DialogInstance.Cancel();

    private void Confirm_Click() => DialogInstance.Close(DialogResult.Ok(true));
}
