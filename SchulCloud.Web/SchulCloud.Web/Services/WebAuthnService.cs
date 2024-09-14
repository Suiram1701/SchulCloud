using Fido2NetLib;
using Microsoft.JSInterop;
using SchulCloud.Web.Services.EventArgs;
using SchulCloud.Web.Services.Exceptions;
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
        return await _runtime.InvokeAsync<bool>($"{WebAuthn}.isSupported", ct);
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
        IJSObjectReference abortControllerRef = await _runtime.InvokeAsync<IJSObjectReference>($"{WebAuthn}.createCredential", ct, operation.OperationReference, options);
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
        IJSObjectReference abortControllerRef = await _runtime.InvokeAsync<IJSObjectReference>($"{WebAuthn}.getCredential", ct, operation.OperationReference, options);
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
            _cancellationTokenRegistration = cancellationToken.Register(OnCancelled, null);
        }

        private async void OnCancelled(object? state, CancellationToken ct)
        {
            _completionSource.SetCanceled(ct);
            await DisposeAsync();
        }

        [JSInvokable("onOperationCompleted")]
        public async void OnOperationCompleted(TResult? result, string? errorMessage)
        {
            if (_disposed)
            {
                return;
            }

            if (result is not null)
            {
                _completionSource.SetResult(result);
            }
            else
            {
                _completionSource.SetException(new WebAuthnException(errorMessage));
            }

            await DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await _cancellationTokenRegistration.DisposeAsync();

                try
                {
                    OperationReference.Dispose();
                    await AbortControllerRef.InvokeVoidAsync("abort", "Operation disposed by server");
                    await AbortControllerRef.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                }

                _disposed = true;
            }
        }
    }
}