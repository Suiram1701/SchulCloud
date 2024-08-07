using Microsoft.AspNetCore.Components;

namespace SchulCloud.Web.Components.Bootstrap.Forms;

/// <summary>
/// A password input in bootstrap style with a floating label and EditForm support. This component includes a button to toggle password visibility.
/// </summary>
public partial class FloatingPasswordInput : FloatingTextInput
{
    /// <summary>
    /// Indicates whether the password is visible.
    /// </summary>
    [Parameter]
    public bool IsPasswordVisible { get; set; }

    protected override string ClassNames => Class ?? string.Empty;

    private void ChangePasswordVisibility_Click()
    {
        if (Disabled)
        {
            return;
        }

        IsPasswordVisible = !IsPasswordVisible;
    }
}
