using Microsoft.JSInterop;
using SchulCloud.Web.Constants;
using static SchulCloud.Web.Constants.JSNames;
using static System.Net.Mime.MediaTypeNames;

namespace SchulCloud.Web.Extensions;

internal static class JSRuntimeExtensions
{
    /// <summary>
    /// Sets the displayed color theme.
    /// </summary>
    /// <param name="runtime">The js runtime of the client.</param>
    /// <param name="theme">The theme to set.</param>
    public static async ValueTask SetColorThemeAsync(this IJSRuntime runtime, Enums.ColorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        await runtime.InvokeVoidAsync($"{ColorTheme}.set", theme.ToString().ToLower());
    }

    /// <summary>
    /// Indicates whether the client supports <see cref="Enums.ColorTheme.Auto"/>.
    /// </summary>
    /// <param name="runtime">The js runtime of the client.</param>
    public static async ValueTask<bool> IsColorAutoColorThemeAvailableAsync(this IJSRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        return await runtime.InvokeAsync<bool>($"{ColorTheme}.autoThemeAvailable");
    }

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
}
