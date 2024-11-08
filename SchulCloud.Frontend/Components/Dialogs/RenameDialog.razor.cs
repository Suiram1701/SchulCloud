using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace SchulCloud.Frontend.Components.Dialogs;

public sealed partial class RenameDialog : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<RenameDialog> Localizer { get; set; } = default!;
    #endregion

    [CascadingParameter]
    private MudDialogInstance DialogInstance { get; set; } = default!;

    private MudForm _renameForm = default!;
    private string _newName = string.Empty;

    [Parameter]
    public string Title { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = default!;

    [Parameter]
    public string? OldName { get; set; }

    [Parameter]
    public IEnumerable<string>? ExcludedNames { get; set; }

    protected override void OnInitialized()
    {
        _newName = OldName ?? string.Empty;
    }

    private void Cancel_Click() => DialogInstance.Cancel();

    private async Task Rename_ClickAsync()
    {
        await _renameForm.Validate();
        if (_renameForm.Errors.Length == 0)
        {
            DialogInstance.Close(DialogResult.Ok(_newName));
        }
    }

    private string? RenameFormNewName_Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Localizer["form_NotEmpty"];
        }
        else if (ExcludedNames is not null)
        {
            foreach (string name in ExcludedNames)
            {
                if (name.Equals(value))
                {
                    return Localizer["form_NewName_AlreadyTaken"];
                }
            }
        }

        return null;
    }
}
