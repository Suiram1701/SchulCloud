using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace SchulCloud.Web.Components.Forms;

/// <summary>
/// A normal MudTextField that can be used for passwords. It has an toggle visibility button as adornment.
/// </summary>
public partial class PasswordField
{
    /// <summary>
    /// CSS classes applied to the input.
    /// </summary>
    [Parameter]
    public string Class { get; set; } = default!;

    /// <summary>
    /// The label of the field.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current value of the input.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = default!;

    /// <summary>
    /// A callback invoked when <see cref="Value"/> changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnValueChanged { get; set; }

    /// <summary>
    /// An expression that is used to identify the EditContext inside the EditContext model.
    /// </summary>
    [Parameter]
    public Expression<Func<string>> For { get; set; } = default!;

    /// <summary>
    /// Gets or sets if the password is currently visible or not
    /// </summary>
    /// <remarks>
    /// Is <c>false</c> by default.
    /// </remarks>
    [Parameter]
    public bool PasswordVisible { get; set; } = false;

    /// <summary>
    /// A callback invoked when <see cref="PasswordVisible"/> changes.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnPasswordVisibleChanged { get; set; }

    /// <summary>
    /// Gets or sets whether this field has an error.
    /// </summary>
    [Parameter]
    public bool Error { get; set; }

    /// <summary>
    /// Gets or sets whether this field is required.
    /// </summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the required error message.
    /// </summary>
    [Parameter]
    public string RequiredError { get; set; } = default!;

    /// <summary>
    /// Captures unmatched parameters.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = [];

    private async Task ToggleVisibility_ClickAsync()
    {
        PasswordVisible = !PasswordVisible;
        await OnPasswordVisibleChanged.InvokeAsync(PasswordVisible).ConfigureAwait(false);
    }
}
