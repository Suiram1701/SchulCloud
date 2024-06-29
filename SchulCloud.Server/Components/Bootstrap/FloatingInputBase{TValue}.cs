using Microsoft.AspNetCore.Components;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace SchulCloud.Server.Components.Bootstrap;

public abstract class FloatingInputBase<TValue> : ComponentBase
{
    [Inject]
    private IIdGenerator IdGenerator { get; set; } = default!;

    /// <summary>
    /// The DOM id of this component.
    /// </summary>
    [Parameter]
    public string Id { get; set; } = default!;

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
    /// Additional classes to apply on the input.
    /// </summary>
    [Parameter]
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// Additional classes to apply on the outer div of the component.
    /// </summary>
    /// <remarks>
    /// This have only effects when an inputs group is used.
    /// </remarks>
    [Parameter]
    public string OuterClass { get; set; } = string.Empty;

    /// <summary>
    /// The currently set value.
    /// </summary>
    [Parameter]
    public virtual TValue Value
    {
        get => _value;
        set
        {
            if (value?.Equals(_value) ?? false)
            {
                return;
            }

            _value = value;
            ValueChanged.InvokeAsync(value);
        }
    }
    protected TValue _value = default!;

    /// <summary>
    /// The event callback for the value.
    /// </summary>
    [Parameter]
    public virtual EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>
    /// Indicates whether this input is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Indicates whether the value of this input is invalid.
    /// </summary>
    [Parameter]
    public bool IsInvalid { get; set; }

    /// <summary>
    /// Indicates whether the value of this input is valid.
    /// </summary>
    [Parameter]
    public bool IsValid { get; set; }

    /// <summary>
    /// An optional expression that can be used to assign this input component to a <see cref="EditContext"/> model.
    /// </summary>
    [Parameter]
    public Expression<Func<TValue>>? For { get; set; }

    /// <summary>
    /// Captures unmatched attribute keys and values.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    protected EditContext? EditContext { get; set; }

    protected virtual bool UseEditForm => EditContext is not null && For is not null;

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = IdGenerator.GetNextId();
        }
    }

    /// <summary>
    /// Returns the CSS class that represents the validation state.
    /// </summary>
    /// <returns>The CSS class.</returns>
    protected virtual string GetValidationClass()
    {
        const string isValid = " is-valid";
        const string isInvalid = " is-invalid";

        if (UseEditForm)
        {
            FieldIdentifier identifier = FieldIdentifier.Create(For!);

            if (!EditContext!.IsModified(identifier))
            {
                return string.Empty;
            }

            if (EditContext.IsValid(identifier))
            {
                return isValid;
            }

            return isInvalid;
        }
        
        if (IsInvalid)
        {
            return isInvalid;
        }
        else if (IsValid)
        {
            return isValid;
        }

        return string.Empty;
    }
}
