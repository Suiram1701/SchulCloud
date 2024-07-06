using Microsoft.AspNetCore.Components;

namespace SchulCloud.Server.Components.Bootstrap.Forms;

/// <summary>
/// A text input in bootstrap style with a floating label and EditForm support.
/// </summary>
public partial class FloatingTextInput : FloatingInputBase<string>
{
    protected async Task OnChangeAsync(ChangeEventArgs e)
    {
        Value = e.Value?.ToString() ?? string.Empty;
        await ValueChangedAsync();
    }
}
