using Microsoft.AspNetCore.Components;
using SchulCloud.Server.Constants;

namespace SchulCloud.Server.Components.Bootstrap.Forms;

public abstract class FloatingInputBase<TValue> : InputBase<TValue>
{
    protected bool ShowValidationFeedback => EditContext is not null && !SuppressValidationFeedback;

    /// <summary>
    /// The displayed label.
    /// </summary>
    [Parameter]
    public required string FloatingLabel { get; set; }

    /// <summary>
    /// The placeholder in the background.
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the validation message shouldn't be displayed.
    /// </summary>
    [Parameter]
    public bool SuppressValidationFeedback { get; set; }

    protected override string? ClassNames =>
        BuildClassNames(
            Class,
            (ExtendedBootstrapClass.FormFloating, true),
            (FieldCssClass, true));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        AdditionalAttributes["placeholder"] = Placeholder;
    }
}
