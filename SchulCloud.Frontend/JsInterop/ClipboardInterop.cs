using Microsoft.JSInterop;
using static SchulCloud.Frontend.JsInterop.JSNames;

namespace SchulCloud.Frontend.JsInterop;

/// <summary>
/// A service that provides an interface to the client-side clipboard-api.
/// </summary>
/// <param name="runtime">The js runtime to use.</param>
public class ClipboardInterop(IJSRuntime runtime)
{
    /// <summary>
    /// Checks whether the client supports the clipboard api.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result of the check.</returns>
    public async ValueTask<bool> IsSupportedAsync(CancellationToken ct = default)
    {
        return await runtime.InvokeAsync<bool>($"{Clipboard}.isSupported", cancellationToken: ct);
    }

    /// <summary>
    /// Copies a text into the clipboard taking a specified MIME type into account.
    /// </summary>
    /// <param name="value">The text to copy into the the clipboard.</param>
    /// <param name="type">The MIME type to use. If <c>null</c> 'text/plain will be used'.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns><c>null</c> if the operation were successful. If not <c>null</c> an error occurred. If <c>NotAllowed</c> were returned the client supports this api but this app doesn't have the permission to use it.</returns>
    public async ValueTask<string?> WriteAsync(string value, string? type, CancellationToken ct = default)
    {
        return await runtime.InvokeAsync<string?>($"{Clipboard}.write", args: [value, type], cancellationToken: ct);
    }

    /// <summary>
    /// Copies text or an image inside of the stream into the clipboard.
    /// </summary>
    /// <param name="stream">The stream of the text or image data.</param>
    /// <param name="type">The MIME type to use. If <c>null</c> 'image/png' will be used regardless whether it an image or text.</param>
    /// <param name="leaveOpen">Indicates whether <paramref name="stream"/> should be left open (closing it would dispose it).</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns><c>null</c> if the operation were successful. If not <c>null</c> an error occurred. If <c>NotAllowed</c> were returned the client supports this api but this app doesn't have the permission to use it.</returns>
    public async ValueTask<string?> WriteAsync(Stream stream, string? type, bool leaveOpen = false, CancellationToken ct = default)
    {
        using DotNetStreamReference streamReference = new(stream, leaveOpen);
        return await runtime.InvokeAsync<string?>($"{Clipboard}.write", args: [streamReference, type], cancellationToken: ct);
    }

    /// <summary>
    /// Copies a text into the clipboard.
    /// </summary>
    /// <param name="value">The text to copy into the clipboard.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns><c>null</c> if the operation were successful. If not <c>null</c> an error occurred. If <c>NotAllowed</c> were returned the client supports this api but this app doesn't have the permission to use it.</returns>
    public async ValueTask<string?> WriteTextAsync(string value, CancellationToken ct = default)
    {
        return await runtime.InvokeAsync<string?>($"{Clipboard}.writeText", args: [value], cancellationToken: ct);
    }
}
