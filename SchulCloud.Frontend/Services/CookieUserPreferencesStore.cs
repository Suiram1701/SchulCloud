using Microsoft.AspNetCore.Localization;
using SchulCloud.Frontend.Enums;
using SchulCloud.Frontend.Services.Interfaces;
using SchulCloud.Frontend.Services.Models;

namespace SchulCloud.Frontend.Services;

/// <summary>
/// An implementation of <see cref="IUserPreferencesStore"/> that uses cookie to store the preferences.
/// </summary>
public class CookieUserPreferencesStore(CookieConsentService cookieConsentService, CookieService cookieService) : IUserPreferencesStore
{
    private readonly string _themeCookieName = ".AspNetCore.ColorTheme";
    private readonly CookieOptions _cookieOptions = new() { Expires = DateTime.UtcNow.AddDays(400) };

    public async Task<ColorTheme> GetPreferredColorThemeAsync(CancellationToken ct = default)
    {
        string? value = await cookieService.GetCookieAsync(_themeCookieName, ct);
        if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value, out ColorTheme result))
        {
            return ColorTheme.Auto;
        }
        return result;
    }

    public async Task SetPreferredColorThemeAsync(ColorTheme theme, CancellationToken ct = default)
    {
        if (await IsFunctionalCookiesAllowedAsync(ct))
        {
            await cookieService.SetCookieAsync(_themeCookieName, theme.ToString(), _cookieOptions, ct);
        }
    }

    public async Task<RequestCulture?> GetPreferredCulturesAsync(CancellationToken ct = default)
    {
        string? value = await cookieService.GetCookieAsync(CookieRequestCultureProvider.DefaultCookieName, ct);
        if (string.IsNullOrWhiteSpace(value) || CookieRequestCultureProvider.ParseCookieValue(value) is not ProviderCultureResult cultureResult)
        {
            return null;
        }

        string? cultureStr = cultureResult.Cultures.FirstOrDefault().Value;
        string? uiCultureStr = cultureResult.UICultures.FirstOrDefault().Value;
        if (string.IsNullOrWhiteSpace(cultureStr) || string.IsNullOrWhiteSpace(uiCultureStr))
        {
            return null;
        }

        return new(cultureStr, uiCultureStr);
    }

    public async Task SetPreferredCulturesAsync(RequestCulture? culture, CancellationToken ct = default)
    {
        if (culture is not null)
        {
            if (await IsFunctionalCookiesAllowedAsync(ct))
            {
                string cookieValue = CookieRequestCultureProvider.MakeCookieValue(culture);
                await cookieService.SetCookieAsync(CookieRequestCultureProvider.DefaultCookieName, cookieValue, _cookieOptions, ct);
            }
        }
        else
        {
            await cookieService.RemoveCookieAsync(CookieRequestCultureProvider.DefaultCookieName, ct: ct);
        }
    }

    /// <summary>
    /// Checks whether the cookies that are used by this service are permitted by the user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result.</returns>
    public async Task<bool> IsFunctionalCookiesAllowedAsync(CancellationToken ct)
    {
        CookieConsentSettings? cookieConsent = await cookieConsentService.GetCookieConsentSettingsAsync(ct);
        return cookieConsent is not null && cookieConsent.Functionality;
    }
}
