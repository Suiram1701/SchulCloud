﻿using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SchulCloud.Web.Constants;

namespace SchulCloud.Web.Components.Bootstrap.Forms;

/// <summary>
/// A default check box in the bootstrap style with EditForm support.
/// </summary>
public partial class CheckBoxInput : InputBase<bool>
{
    private bool _oldIndeterminate = false;

    /// <summary>
    /// Indicates whether this checkbox is in indeterminate state.
    /// </summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>
    /// Indicates whether this
    /// </summary>
    [Parameter]
    public bool Reverse { get; set; }

    /// <summary>
    /// The label of this input.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = default!;

    /// <summary>
    /// Indicates whether these elements should align inline.
    /// </summary>
    [Parameter]
    public bool Inline { get; set; }

    protected override string ClassNames =>
        BuildClassNames(
            Class,
            (BootstrapClass.FormCheck, true),
            (BootstrapClass.FormCheckReverse, Reverse),
            (ExtendedBootstrapClass.FormCheckInline, Inline));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        AdditionalAttributes["checked"] = Value;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Indeterminate != _oldIndeterminate)
        {
            await JSRuntime.InvokeVoidAsync($"{JSNames.BlazorBootstrapCheckBox}.setIndeterminate", Id, Indeterminate);
            _oldIndeterminate = Indeterminate;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnChangeAsync(ChangeEventArgs e)
    {
        _ = bool.TryParse(e.Value?.ToString(), out bool result);
        Value = result;

        await ValueChangedAsync();
    }
}
