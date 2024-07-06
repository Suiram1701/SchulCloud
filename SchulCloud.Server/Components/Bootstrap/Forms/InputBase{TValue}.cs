using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace SchulCloud.Server.Components.Bootstrap.Forms;

public class InputBase<TValue> : BlazorBootstrapComponentBase
{
    private TValue _oldValue = default!;

    protected FieldIdentifier _fieldIdentifier;

    /// <summary>
    /// The currently set value.
    /// </summary>
    [Parameter]
    public TValue Value { get; set; } = default!;

    /// <summary>
    /// An expression that refers to the value.
    /// </summary>
    [Parameter]
    public Expression<Func<TValue>> ValueExpression { get; set; } = default!;

    /// <summary>
    /// The event callback for the value.
    /// </summary>
    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>
    /// Indicates whether this input is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    [CascadingParameter]
    protected EditContext? EditContext { get; set; }

    protected string FieldCssClass => EditContext?.FieldCssClass(_fieldIdentifier) ?? string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AdditionalAttributes ??= [];
        _fieldIdentifier = FieldIdentifier.Create(ValueExpression);

        _oldValue = Value;
    }

    protected override async Task OnParametersSetAsync()
    {
        AdditionalAttributes["disabled"] = Disabled;

        if (!_oldValue?.Equals(Value) ?? Value is not null)
        {
            await ValueChangedAsync();
        }

        await base.OnParametersSetAsync();
    }

    protected async Task ValueChangedAsync()
    {
        await ValueChanged.InvokeAsync(Value);
        EditContext?.NotifyFieldChanged(_fieldIdentifier);

        _oldValue = Value;
    }
}
