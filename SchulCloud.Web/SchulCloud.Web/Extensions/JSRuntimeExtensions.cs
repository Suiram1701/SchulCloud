using Microsoft.JSInterop;
using SchulCloud.Web.Constants;
using static SchulCloud.Web.Constants.JSNames;

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
    /// <param name="fileName">The displayed name of the file.</param>
    /// <param name="stream">The stream to download.</param>
    /// <param name="leaveOpen">Indicates whether the <paramref name="stream"/> should stay open after the transmission.</param>
    public static async ValueTask DownloadFileAsync(this IJSRuntime runtime, string fileName, Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        using DotNetStreamReference streamRef = new(stream, leaveOpen);
        await runtime.InvokeVoidAsync($"{JSNames.File}.download", fileName, streamRef);
    }

    /// <summary>
    /// Downloads a file on a specified url to the client.
    /// </summary>
    /// <param name="runtime">The js runtime of the client.</param>
    /// <param name="fileName">The displayed name of the file.</param>
    /// <param name="filePath">The url of the file.</param>
    public static async ValueTask DownloadFileAsync(this IJSRuntime runtime, string fileName, Uri filePath)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        await runtime.InvokeVoidAsync($"{JSNames.File}.downloadFromUrl", fileName, filePath);
    }
}
