﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace SchulCloud.Web.Components.Bootstrap.Validation;

public partial class BsValidationFeedback : ComponentBase, IDisposable
{
    private bool _disposedValue;

    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    /// <summary>
    /// An expression that specified the field for that this component displays messages for.
    /// </summary>
    [Parameter]
    public Expression<Func<object>> For { get; set; } = default!;

    /// <summary>
    /// The identifier of the field this component displays messages for.
    /// </summary>
    [Parameter]
    public FieldIdentifier? Identifier { get; set; }

    protected override void OnInitialized()
    {
        EditContext.OnValidationStateChanged += OnValidationStateChanged;
    }

    protected override void OnParametersSet()
    {
        if (For is null && Identifier is null)
        {
            throw new InvalidOperationException($"{nameof(For)} or {nameof(Identifier)} have to be provided.");
        }
        else if (For is not null)
        {
            Identifier = FieldIdentifier.Create(For!);
        }

        if (EditContext is null)
        {
            throw new InvalidOperationException($"{nameof(BsValidationFeedback)} have to be placed inside of an {nameof(EditContext)}.");
        }
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        StateHasChanged();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                EditContext.OnValidationStateChanged -= OnValidationStateChanged;
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