using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using SchulCloud.Server.Enums;
using SchulCloud.Server.Options;
using SchulCloud.Server.Utils.Interfaces;
using System.Globalization;

namespace SchulCloud.Server.Components.Layouts;

[AllowAnonymous]
public partial class AnonLayout : LayoutComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<AnonLayout> Localizer { get; set; } = default!;

    [Inject]
    private ILocalStorageService BrowserStorage { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private IOptions<Options.LocalizationOptions> LocalizationOptionsAccessor { get; set; } = default!;

    [Inject]
    private IOptionsSnapshot<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;

    [Inject]
    private ICookieHelper CookieHelper { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private CultureInfo? _activeCulture;

    private ColorTheme _activeColorTheme;

    private bool _autoModeAvailable = true;

    private const string _themeKey = ".AspNetCore.Theme";

    protected override void OnInitialized()
    {
        string? cultureCookieValue = HttpContextAccessor.HttpContext?.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
        if (!string.IsNullOrEmpty(cultureCookieValue))
        {
            _activeCulture = Thread.CurrentThread.CurrentUICulture;
        }

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _autoModeAvailable = await JSRuntime.InvokeAsync<bool>("autoColorThemeAvailable");

            ColorTheme theme = await BrowserStorage.GetItemAsync<ColorTheme>(_themeKey);
            if (theme == ColorTheme.Auto && !_autoModeAvailable)
            {
                _activeColorTheme = ColorTheme.Light;
            }
            else
            {
                _activeColorTheme = theme;
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task CultureChange_ClickAsync(CultureInfo? culture)
    {
        if (_activeCulture?.Name == culture?.Name)
        {
            return;
        }

        if (culture is not null)
        {
            CookieOptions options = new() { MaxAge = TimeSpan.FromDays(400) };

            string cookieValue = CookieRequestCultureProvider.MakeCookieValue(new(culture));
            await CookieHelper.SetCookieAsync(CookieRequestCultureProvider.DefaultCookieName, cookieValue, options);
        }
        else
        {
            await CookieHelper.RemoveCookieAsync(CookieRequestCultureProvider.DefaultCookieName);
        }

        _activeCulture = culture;

        NavigationManager.Refresh(forceReload: true);
    }

    private async Task ColorThemeChange_ClickAsync(ColorTheme theme)
    {
        if (_activeColorTheme == theme)
        {
            return;
        }

        if (theme == ColorTheme.Auto)
        {
            await BrowserStorage.RemoveItemAsync(_themeKey);
        }
        else
        {
            await BrowserStorage.SetItemAsync(_themeKey, theme);
        }

        await DisplayColorThemeAsync(theme);
    }

    private async ValueTask DisplayColorThemeAsync(ColorTheme theme)
    {
        if (!_autoModeAvailable && theme == ColorTheme.Auto)
        {
            theme = ColorTheme.Light;
        }

        _activeColorTheme = theme;
        await JSRuntime.InvokeVoidAsync("setTheme", theme.ToString().ToLower());
    }
}
