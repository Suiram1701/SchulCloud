using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SchulCloud.Web.Constants;

namespace SchulCloud.Web.Components.Bootstrap.Validation;

public class BsFieldCssProvider : ComponentBase, IDisposable
{
    private bool _disposedValue;
    private readonly CssProvider _cssProvider = new();

    /// <summary>
    /// Indicates whether the valid state should be disabled.
    /// </summary>
    /// <remarks>
    /// This is by default <c>false</c>.
    /// </remarks>
    [Parameter]
    public bool DisableValidState { get; set; }

    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    protected override void OnParametersSet()
    {
        _cssProvider.DisableValidState = DisableValidState;
    }

    protected override void OnInitialized()
    {
        if (EditContext is null)
        {
            throw new InvalidOperationException($"The {nameof(BsFieldCssProvider)} component can only be used inside of an edit form.");
        }

        EditContext.SetFieldCssClassProvider(_cssProvider);
        EditContext.OnValidationRequested += OnValidationRequest;
    }

    private void OnValidationRequest(object? sender, ValidationRequestedEventArgs e)
    {
        if (!_cssProvider.FirstValidated)
        {
            _cssProvider.FirstValidated = true;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                EditContext.OnValidationRequested -= OnValidationRequest;
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private class CssProvider : FieldCssClassProvider
    {
        public bool FirstValidated { get; set; }

        public bool DisableValidState { get; set; }

        public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
        {
            if (!editContext.IsModified(fieldIdentifier) || !FirstValidated)
            {
                return string.Empty;
            }
            else if (editContext.IsValid(fieldIdentifier))
            {
                return !DisableValidState
                    ? ExtendedBootstrapClass.IsValid
                    : string.Empty;
            }
            else
            {
                return ExtendedBootstrapClass.IsInvalid;
            }
        }
    }
}
