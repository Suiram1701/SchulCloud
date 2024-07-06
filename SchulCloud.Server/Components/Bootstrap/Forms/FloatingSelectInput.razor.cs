using Microsoft.AspNetCore.Components;
using SchulCloud.Server.Constants;

namespace SchulCloud.Server.Components.Bootstrap.Forms;

/// <summary>
/// A select input in bootstrap style with floating label and EditForm support.
/// </summary>
/// <typeparam name="TValue">The type of the selectable values.</typeparam>
public partial class FloatingSelectInput<TValue> : FloatingInputBase<TValue>
{
    /// <summary>
    /// Values to show as an options
    /// </summary>
    [Parameter]
    public required IEnumerable<TValue> Values { get; set; }

    /// <summary>
    /// Values to disable.
    /// </summary>
    /// <remarks>
    /// Every value that this contains also have to be contained in <see cref="Values"/>
    /// </remarks>
    [Parameter]
    public IEnumerable<TValue> DisabledValues { get; set; } = [];

    /// <summary>
    /// Specifies a selector that is used to determine a unique id of the item
    /// </summary>
    /// <remarks>
    /// By default is <see cref="TValue.ToString"/> used.
    /// </remarks>
    [Parameter]
    public Func<TValue, string> IdSelector { get; set; } = default!;

    /// <summary>
    /// Specifies a default template that will be shown when no item is selected (<see cref="Value"/> is <c>null</c>).
    /// </summary>
    /// <remarks>
    /// This item isn't selectable.
    /// </remarks>
    [Parameter]
    public RenderFragment? DefaultTemplate { get; set; }

    /// <summary>
    /// A template that will be used to represent the options.
    /// </summary>
    /// <remarks>
    /// When <c>null</c> <see cref="TValue.ToString()"/> will be used to represent the item.
    /// </remarks>
    [Parameter]
    public RenderFragment<TValue>? ItemTemplate { get; set; }

    protected override string ClassNames => BuildClassNames(Class, (ExtendedBootstrapClass.FormFloating, true));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Values.Any(v => v is null))
        {
            throw new ArgumentNullException(nameof(Values), $"null values aren't allowed in {nameof(Values)}");
        }

        if (!DisabledValues.All(v => Values.Contains(v)))
        {
            throw new InvalidOperationException($"Every value of {nameof(DisabledValues)} have to be contained in {nameof(Values)}.");
        }

        if (Value is null && DefaultTemplate is null)
        {
            Value = Values.First();
        }

        IdSelector ??= value => value!.ToString()!;
    }

    private async Task OnSelectionChangedAsync(ChangeEventArgs e)
    {
        string valueId = (string)e.Value!;

        TValue? newValue = Values.FirstOrDefault(value => IdSelector(value).Equals(valueId))
            ?? throw new KeyNotFoundException($"Unable to find item with id {valueId}.");

        Value = newValue;
        await ValueChangedAsync();
    }
}
