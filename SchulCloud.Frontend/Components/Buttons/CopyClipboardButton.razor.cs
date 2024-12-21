using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SchulCloud.Frontend.Extensions;
using SchulCloud.Frontend.JsInterop;

namespace SchulCloud.Frontend.Components.Buttons;

public partial class CopyClipboardButton : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<CopyClipboardButton> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private ClipboardInterop ClipboardInterop { get; set; } = default!;
    #endregion

    private bool _copiedTooltipVisible;

    [Parameter]
    public string? CopyText { get; set; }

    [Parameter]
    public Stream? CopyStream { get; set; }

    public string? CopyType { get; set; }

    public bool IsSupported { get; private set; } = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            IsSupported = await ClipboardInterop.IsSupportedAsync().AsTask();
        }
    }

    private async Task Copy_ClickAsync()
    {
        string? error = (CopyText, CopyStream, CopyType) switch
        {
            { CopyText: not null, CopyType: null } => await ClipboardInterop.WriteTextAsync(CopyText!).AsTask(),
            { CopyText: not null } => await ClipboardInterop.WriteAsync(CopyText, CopyType).AsTask(),
            { CopyStream: not null } => await ClipboardInterop.WriteAsync(CopyStream, CopyType, leaveOpen: true).AsTask(),
            _ => throw new InvalidOperationException("Nothing to copy is set.")
        };

        switch (error)
        {
            case null:
                _copiedTooltipVisible = true;
                StateHasChanged();

                await Task.Delay(1000);

                _copiedTooltipVisible = false;
                StateHasChanged();
                break;
            case "NotAllowed":
                IsSupported = false;
                Snackbar.AddError(Localizer["error_NotAllowed"]);
                break;
            default:
                Snackbar.AddError(Localizer["error", error]);
                break;
            }
    }
}
