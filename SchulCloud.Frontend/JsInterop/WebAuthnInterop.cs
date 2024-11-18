using Fido2NetLib;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;
using SchulCloud.Frontend.Services.Exceptions;
using static SchulCloud.Frontend.Constants.JSNames;

namespace SchulCloud.Frontend.JsInterop;

/// <summary>
/// A service that provides an interface to the client-side webauthn api.
/// </summary>
public class WebAuthnInterop(IJSRuntime runtime)
{
    /// <summary>
    /// Indicates whether the client supports webauthn.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result.</returns>
    public async ValueTask<bool> IsSupportedAsync(CancellationToken ct = default)
    {
        return await runtime.InvokeAsync<bool>($"{WebAuthn}.isSupported", ct);
    }

    /// <summary>
    /// Creates a new credential on the client.
    /// </summary>
    /// <param name="options">The options to use.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="WebAuthnException"></exception>
    public async Task<AuthenticatorAttestationRawResponse> CreateCredentialAsync(CredentialCreateOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ct.ThrowIfCancellationRequested();

        WebAuthnOperation<AuthenticatorAttestationRawResponse> operation = new(ct);
        IJSObjectReference abortControllerRef = await runtime.InvokeAsync<IJSObjectReference>($"{WebAuthn}.createCredential", ct, operation.OperationReference, options);
        operation.AbortControllerRef = abortControllerRef;

        return await operation.Task;
    }

    /// <summary>
    /// Gets a credential from the client.
    /// </summary>
    /// <param name="options">The options to use.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result of the request.</returns>
    /// <exception cref="WebAuthnException"></exception>
    public async Task<AuthenticatorAssertionRawResponse> GetCredentialAsync(AssertionOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ct.ThrowIfCancellationRequested();

        WebAuthnOperation<AuthenticatorAssertionRawResponse> operation = new(ct);
        IJSObjectReference abortControllerRef = await runtime.InvokeAsync<IJSObjectReference>($"{WebAuthn}.getCredential", ct, operation.OperationReference, options);
        operation.AbortControllerRef = abortControllerRef;

        return await operation.Task;
    }

    private sealed class WebAuthnOperation<TResult> : IAsyncDisposable
    {
        public DotNetObjectReference<WebAuthnOperation<TResult>> OperationReference { get; }

        public IJSObjectReference AbortControllerRef { get; set; } = default!;

        public Task<TResult> Task => _completionSource.Task;

        private bool _disposed;
        private readonly TaskCompletionSource<TResult> _completionSource;
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;

        public WebAuthnOperation(CancellationToken cancellationToken)
        {
            OperationReference = DotNetObjectReference.Create(this);

            _completionSource = new();
            _cancellationTokenRegistration = cancellationToken.Register(CancellationToken_OnCancelled, null);
        }

        private async void CancellationToken_OnCancelled(object? state, CancellationToken ct)
        {
            _completionSource.SetCanceled(ct);
            await DisposeAsync(abortClientSide: true);
        }

        [JSInvokable("operationCompleted")]
        public async void OnOperationCompleted(TResult? result, string? errorMessage)
        {
            if (!_disposed)
            {
                if (result is not null)
                {
                    _completionSource.SetResult(result);
                }
                else
                {
                    _completionSource.SetException(new WebAuthnException(errorMessage));
                }

                await DisposeAsync(abortClientSide: false);
            }
        }

        public async ValueTask DisposeAsync() => await DisposeAsync(true);

        private async ValueTask DisposeAsync(bool abortClientSide)
        {
            if (!_disposed)
            {
                await _cancellationTokenRegistration.DisposeAsync();

                try
                {
                    OperationReference.Dispose();

                    if (abortClientSide)     // If the operation ends on client side first an abort signal will cause an exception.
                    {
                        await AbortControllerRef.InvokeVoidAsync("abort", "Operation disposed by server.");
                        await AbortControllerRef.DisposeAsync();
                    }
                }
                catch (JSDisconnectedException)
                {
                }

                _disposed = true;
            }
        }
    }
}