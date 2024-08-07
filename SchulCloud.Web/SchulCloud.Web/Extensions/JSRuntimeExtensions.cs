using Microsoft.JSInterop;
using static SchulCloud.Web.Constants.JSNames;

namespace SchulCloud.Web.Extensions;

internal static class JSRuntimeExtensions
{
    public static async ValueTask SetColorThemeAsync(this IJSRuntime runtime, Enums.ColorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(runtime, nameof(runtime));
        await runtime.InvokeVoidAsync($"{ColorTheme}.set", theme.ToString().ToLower());
    }

    public static async ValueTask<bool> IsColorAutoColorThemeAvailableAsync(this IJSRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime, nameof(runtime));
        return await runtime.InvokeAsync<bool>($"{ColorTheme}.autoThemeAvailable");
    }
}
