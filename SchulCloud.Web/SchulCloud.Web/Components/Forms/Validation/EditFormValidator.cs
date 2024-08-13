using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace SchulCloud.Web.Components.Forms.Validation;

public class EditFormValidator<TValue> : ComponentBase, IDisposable
{
    private bool _disposedValue;
    private FieldIdentifier _fieldIdentifier;
    private ValidationMessageStore _messageStore = default!;

    /// <summary>
    /// An expression that specified the field that should validated.
    /// </summary>
    [Parameter]
    public required Expression<Func<TValue>> For { get; set; }

    /// <summary>
    /// Get called when the value should be validated.
    /// </summary>
    /// <remarks>
    /// The return value is the list of validation errors.
    /// </remarks>
    [Parameter]
    public required Func<EditContext, FieldIdentifier, Task<IEnumerable<string>>> ValidateAsync { get; set; }

    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (EditContext == null)
        {
            throw new InvalidOperationException($"{nameof(EditFormValidator<TValue>)} have to be placed inside of an {nameof(EditContext)}.");
        }

        ArgumentNullException.ThrowIfNull(ValidateAsync, nameof(ValidateAsync));

        ArgumentNullException.ThrowIfNull(For, nameof(For));
        _fieldIdentifier = FieldIdentifier.Create(For);
    }

    protected override void OnInitialized()
    {
        _messageStore = new(EditContext);
        EditContext.OnValidationRequested += OnValidationRequestedAsync;
    }

    private async void OnValidationRequestedAsync(object? sender, ValidationRequestedEventArgs e)
    {
        _messageStore.Clear(_fieldIdentifier);

        IEnumerable<string> validationErrors = await ValidateAsync(EditContext, _fieldIdentifier);
        if (validationErrors.Any())
        {
            _messageStore.Add(_fieldIdentifier, validationErrors);
        }

        EditContext.NotifyFieldChanged(_fieldIdentifier);
        EditContext.NotifyValidationStateChanged();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                EditContext.OnValidationRequested -= OnValidationRequestedAsync;
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
