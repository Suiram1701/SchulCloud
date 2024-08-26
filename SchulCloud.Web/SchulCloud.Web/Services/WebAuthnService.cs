using Fido2NetLib;
using Microsoft.JSInterop;
using SchulCloud.Web.Services.EventArgs;
using static SchulCloud.Web.Constants.JSNames;

namespace SchulCloud.Web.Services;

/// <summary>
/// A service that provides an interface to the client-side webauthn api.
/// </summary>
public class WebAuthnService(IJSRuntime runtime)
{
    private readonly IJSRuntime _runtime = runtime;

    /// <summary>
    /// Indicates whether the client supports webauthn.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result.</returns>
    public async ValueTask<bool> IsSupportedAsync(CancellationToken ct = default)
    {
        return await _runtime.InvokeAsync<bool>($"{WebAuthn}.isSupported").ConfigureAwait(false);
    }

    /// <summary>
    /// Begins an operation to create a new credential.
    /// </summary>
    /// <param name="options">The options to use.</param>
    /// <param name="completedCallback">The callback when the operation is completed.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The operation. To abort the operation call <see cref="IAsyncDisposable.DisposeAsync"/>.</returns>
    public async ValueTask<IAsyncDisposable> StartCreateCredentialAsync(
        CredentialCreateOptions options,
        EventHandler<WebAuthnCompletedEventArgs<AuthenticatorAttestationRawResponse>> completedCallback,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(completedCallback);
        ArgumentNullException.ThrowIfNull(ct);

        WebAuthnOperation<AuthenticatorAttestationRawResponse> operation = new(completedCallback);
        IJSObjectReference abortControllerRef = await _runtime.InvokeAsync<IJSObjectReference>($"{WebAuthn}.createCredential", ct, operation.OperationReference, options).ConfigureAwait(false);
        operation.AbortControllerRef = abortControllerRef;

        return operation;
    }

    /// <summary>
    /// Begins an operation to get a credential from the client.
    /// </summary>
    /// <param name="options">The options to use.</param>
    /// <param name="completedCallback">The callback when the operation is completed.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The operation. To abort the operation call <see cref="IAsyncDisposable.DisposeAsync"/>.</returns>
    public async ValueTask<IAsyncDisposable> StartGetCredentialAsync(
        AssertionOptions options,
        EventHandler<WebAuthnCompletedEventArgs<AuthenticatorAssertionRawResponse>> completedCallback,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(completedCallback);
        ArgumentNullException.ThrowIfNull(ct);

        WebAuthnOperation<AuthenticatorAssertionRawResponse> operation = new(completedCallback);
        IJSObjectReference abortControllerRef = await _runtime.InvokeAsync<IJSObjectReference>($"{WebAuthn}.getCredential", ct, operation.OperationReference, options).ConfigureAwait(false);
        operation.AbortControllerRef = abortControllerRef;

        return operation;
    }

    private sealed class WebAuthnOperation<TResult> : IAsyncDisposable
    {
        public DotNetObjectReference<WebAuthnOperation<TResult>> OperationReference { get; }

        public IJSObjectReference AbortControllerRef { get; set; } = default!;

        private readonly EventHandler<WebAuthnCompletedEventArgs<TResult>> _completedCallback;

        public WebAuthnOperation(EventHandler<WebAuthnCompletedEventArgs<TResult>> completedCallback)
        {
            OperationReference = DotNetObjectReference.Create(this);
            _completedCallback = completedCallback;
        }

        [JSInvokable("onOperationCompleted")]
        public async void OnOperationCompleted(TResult? result, string? errorMessage)
        {
            WebAuthnCompletedEventArgs<TResult> eventArgs = new(result, errorMessage);
            _completedCallback.Invoke(this, eventArgs);

            await DisposeAsync().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await AbortControllerRef.InvokeVoidAsync("abort", "Operation disposed by server").ConfigureAwait(false);
            await AbortControllerRef.DisposeAsync().ConfigureAwait(false);

            OperationReference.Dispose();
        }
    }
}