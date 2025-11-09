using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using SchulCloud.Identity.Enums;
using System.Globalization;
using MudBlazor.FontIcons.MaterialSymbols;

namespace SchulCloud.Frontend.Components.Layouts;

public sealed partial class AnonymousLayout : BaseLayout
{
    #region Injections
    [Inject]
    private IStringLocalizer<AnonymousLayout> Localizer { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;
    #endregion

    protected override async Task<(ColorTheme, CultureInfo?)> GetUserPreferencesAsync()
    {
        ColorTheme theme = await UserPreferences.GetPreferredColorThemeAsync();
        RequestCulture? cultures = await UserPreferences.GetPreferredCulturesAsync();

        return (theme, cultures?.UICulture);
    }

    private async Task ChangeColorTheme_ClickAsync(ColorTheme theme)
    {
        _colorTheme = theme;
        _isDarkMode = _colorTheme == ColorTheme.Auto
            ? await _themeProvider.GetSystemDarkModeAsync()
            : _colorTheme == ColorTheme.Dark;

        await UserPreferences.SetPreferredColorThemeAsync(theme);
    }

    private async Task ChangeCulture_ClickAsync(CultureInfo? culture)
    {
        RequestCulture? cultures = culture is not null
            ? new RequestCulture(culture)
            : null;
        await UserPreferences.SetPreferredCulturesAsync(cultures);

        NavigationManager.Refresh(forceReload: true);
    }
    
    private static (string icon, string localizerKey) GetColorThemeInfo(ColorTheme theme)
    {
        string icon = theme == ColorTheme.Auto
            ? Outlined.Contrast
            : $"material-symbols-outlined/{theme}_mode".ToLowerInvariant();
        var key = $"theme_{theme}Mode";

        return (icon, key);
    }

    private string GetFlagImgUrl(CultureInfo culture) =>  Assets[$"/_content/flags/{culture.Name.Replace('-', '_')}.svg"];
}