using Microsoft.JSInterop;
using MudBlazor;
using Newtonsoft.Json;
using SchulCloud.Frontend.Services.Models;

namespace SchulCloud.Frontend.Services;

/// <summary>
/// A service that provides information which cookie types are allowed by the user.
/// </summary>
/// <param name="logger">The logger to use for errors.</param>
/// <param name="cookieService">The cookie service to use.</param>
public class CookieConsentService(ILogger<CookieConsentService> logger, IJSRuntime runtime, CookieService cookieService)
{
    /// <summary>
    /// Gets the settings that were chosen by the user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The settings. If <c>null</c> the user hasn't set anything yet.</returns>
    public async Task<CookieConsentSettings?> GetCookieConsentSettingsAsync(CancellationToken ct = default)
    {
        string? value = await cookieService.GetCookieAsync("cookie_consent_level", ct);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string rawJson = Uri.UnescapeDataString(value);
        try
        {
            return JsonConvert.DeserializeObject<CookieConsentSettings>(rawJson);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "An error occurred while parsing cookie consent json: '{json}'", rawJson);
            return null;
        }
    }

    /// <summary>
    /// Opens the cookie preferences center dialog.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    public async Task OpenPreferencesCenterAsync(CancellationToken ct = default)
    {
        await runtime.InvokeVoidAsyncIgnoreErrors("cookieconsent.openPreferencesCenter", ct);
    }
}
