using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SchulCloud.Web.Constants;
using static SchulCloud.Web.Constants.JSNames;
using static System.Net.Mime.MediaTypeNames;

namespace SchulCloud.Web.Extensions;

internal static class JSRuntimeExtensions
{
    /// <summary>
    /// Downloads a specified stream to the client.
    /// </summary>
    /// <param name="runtime">The js runtime of the client.</param>
    /// <param name="stream">The stream to download.</param>
    /// <param name="fileName">The displayed name of the file.</param>
    /// <param name="mimeType">The mime type of the file.</param>
    /// <param name="convertNlChars">Indicates whether new line character should be converted to the clients file system.</param>
    /// <param name="leaveOpen">Indicates whether the <paramref name="stream"/> should stay open after the transmission.</param>
    public static async ValueTask DownloadFileAsync(this IJSRuntime runtime, Stream stream, string fileName, string mimeType = Application.Octet, bool convertNlChars = false, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        string endings = convertNlChars
            ? "native"
            : "transparent";
        using DotNetStreamReference streamRef = new(stream, leaveOpen);
        await runtime.InvokeVoidAsync($"{JSNames.File}.download", streamRef, fileName, mimeType, endings);
    }

    /// <summary>
    /// Downloads a file on a specified url to the client.
    /// </summary>
    /// <param name="runtime">The js runtime of the client.</param>
    /// <param name="filePath">The url of the file.</param>
    /// <param name="fileName">The displayed name of the file.</param>
    public static async ValueTask DownloadFileAsync(this IJSRuntime runtime, Uri filePath, string fileName)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        await runtime.InvokeVoidAsync($"{JSNames.File}.downloadFromUrl", fileName, filePath);
    }

    /// <summary>
    /// Calls submit on a form element.
    /// </summary>
    /// <param name="runtime">The runtime to use.</param>
    /// <param name="reference">The element to call submit on. If not a form element a error will be logged on client-side.</param>
    public static async ValueTask FormSubmitAsync(this IJSRuntime runtime, ElementReference reference)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        await runtime.InvokeVoidAsyncIgnoreErrors($"{ElementHelpers}.formSubmit", reference).ConfigureAwait(false);
    }
}
