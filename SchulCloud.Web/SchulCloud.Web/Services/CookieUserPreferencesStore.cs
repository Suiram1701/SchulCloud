using Microsoft.AspNetCore.Localization;
using SchulCloud.Web.Enums;
using SchulCloud.Web.Services.Interfaces;

namespace SchulCloud.Web.Services;

/// <summary>
/// An implementation of <see cref="IUserPreferencesStore"/> that uses cookie to store the preferences.
/// </summary>
public class CookieUserPreferencesStore(CookieService cookieService) : IUserPreferencesStore
{
    private readonly string _themeCookieName = ".AspNetCore.ColorTheme";
    private readonly TimeSpan _maxCookieAge = TimeSpan.FromDays(400);

    private readonly CookieService _cookieService = cookieService;

    public async Task<ColorTheme> GetPreferredColorThemeAsync(CancellationToken ct = default)
    {
        string? value = await _cookieService.GetCookieAsync(_themeCookieName, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value, out ColorTheme result))
        {
            return ColorTheme.Auto;
        }
        return result;
    }

    public async Task SetPreferredColorThemeAsync(ColorTheme theme, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(theme);

        await _cookieService.SetCookieAsync(_themeCookieName, theme.ToString(), new()
        {
            Expires = DateTime.UtcNow.Add(_maxCookieAge)
        }, ct).ConfigureAwait(false);
    }

    public async Task<RequestCulture?> GetPreferredCulturesAsync(CancellationToken ct = default)
    {
        string? value = await _cookieService.GetCookieAsync(CookieRequestCultureProvider.DefaultCookieName, ct).ConfigureAwait(false);
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
            string cookieValue = CookieRequestCultureProvider.MakeCookieValue(culture);

            await _cookieService.SetCookieAsync(CookieRequestCultureProvider.DefaultCookieName, cookieValue, new()
            {
                Expires = DateTime.UtcNow.Add(_maxCookieAge)
            }, ct).ConfigureAwait(false);
        }
        else
        {
            await _cookieService.RemoveCookieAsync(CookieRequestCultureProvider.DefaultCookieName, ct: ct).ConfigureAwait(false);
        }
    }
}
