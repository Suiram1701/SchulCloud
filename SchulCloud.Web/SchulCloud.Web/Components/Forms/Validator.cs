using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace SchulCloud.Web.Components.Forms;

public class Validator<TValue> : ComponentBase, IDisposable
{
    private bool _disposedValue;
    private FieldIdentifier _fieldIdentifier;
    private ValidationMessageStore _messageStore = default!;

    /// <summary>
    /// An expression that specified the field that should validated.
    /// </summary>
    [Parameter]
    public Expression<Func<TValue>> For { get; set; } = default!;

    /// <summary>
    /// Gets called when the value should be validated.
    /// </summary>
    /// <remarks>
    /// The return value is the list of validation errors.
    /// </remarks>
    [Parameter]
    public Func<IEnumerable<string>?>? Validate { get; set; }

    /// <summary>
    /// Get called when the value should be validated.
    /// </summary>
    /// <remarks>
    /// The return value is the list of validation errors.
    /// </remarks>
    [Parameter]
    public Func<Task<IEnumerable<string>?>>? ValidateAsync { get; set; }

    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (EditContext == null)
        {
            throw new InvalidOperationException($"{nameof(Validator<TValue>)} have to be placed inside of an {nameof(EditContext)}.");
        }

        ArgumentNullException.ThrowIfNull(For, nameof(For));
        if (Validate is null && ValidateAsync is null)
        {
            throw new InvalidOperationException($"Nether {nameof(Validate)} or {nameof(ValidateAsync)} where specified.");
        }

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

        IEnumerable<string>? validationErrors;
        if (Validate is not null)
        {
            validationErrors = Validate();
        }
        else if (ValidateAsync is not null)
        {
            validationErrors = await ValidateAsync();
        }
        else
        {
            throw new InvalidOperationException($"Nether {nameof(Validate)} or {nameof(ValidateAsync)} where specified.");
        }

        if (validationErrors?.Any() ?? false)
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
