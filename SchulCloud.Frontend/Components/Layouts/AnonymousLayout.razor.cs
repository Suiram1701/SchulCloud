using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Identity.Enums;
using SchulCloud.Frontend.Options;
using SchulCloud.Frontend.Services.Interfaces;
using System.Globalization;
using MudBlazor.FontIcons.MaterialSymbols;
using MudBlazor;

namespace SchulCloud.Frontend.Components.Layouts;

public sealed partial class AnonymousLayout : LayoutComponentBase, IDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<AnonymousLayout> Localizer { get; set; } = default!;

    [Inject]
    private IUserPreferencesStore UserPreferences { get; set; } = default!;

    [Inject]
    private IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;

    [Inject]
    private IOptions<RequestLocalizationOptions> LocalizationOptionsAccessor { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private PersistentComponentState ComponentState { get; set; } = default!;
    #endregion

    private RequestLocalizationOptions LocalizationOptions => LocalizationOptionsAccessor.Value;

    private MudThemeProvider _themeProvider = default!;

    private bool IsAutoColorTheme => _colorTheme == ColorTheme.Auto;
    private bool _isDarkMode;

    private ColorTheme _colorTheme;
    private CultureInfo? _culture;

    private PersistingComponentStateSubscription? _stateSubscription;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext is not null)
        {
            _colorTheme = await UserPreferences.GetPreferredColorThemeAsync();

            RequestCulture? cultures = await UserPreferences.GetPreferredCulturesAsync();
            _culture = cultures?.UICulture;

            _stateSubscription = ComponentState.RegisterOnPersisting(() =>
            {
                ComponentState.PersistAsJson(nameof(_colorTheme), _colorTheme);
                ComponentState.PersistAsJson(nameof(_culture), _culture?.ToString());

                return Task.CompletedTask;
            });
        }
        else
        {
            ComponentState.TryTakeFromJson(nameof(_colorTheme), out _colorTheme);
            if (ComponentState.TryTakeFromJson(nameof(_culture), out string? culture))
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    _culture = CultureInfo.GetCultureInfo(culture!);
                }
            }
        }

        _isDarkMode = _colorTheme == ColorTheme.Dark;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && IsAutoColorTheme)
        {
            _isDarkMode = await _themeProvider.GetSystemPreference();
            StateHasChanged();

            await _themeProvider.WatchSystemPreference(darkMode =>
            {
                _isDarkMode = darkMode;
                StateHasChanged();

                return Task.CompletedTask;
            });
        }
    }

    private async Task ChangeColorTheme_ClickAsync(ColorTheme theme)
    {
        _colorTheme = theme;
        _isDarkMode = _colorTheme == ColorTheme.Auto
            ? await _themeProvider.GetSystemPreference()
            : _colorTheme == ColorTheme.Dark;

        await UserPreferences.SetPreferredColorThemeAsync(theme);
    }

    private async Task ChangeCulture_ClickAsync(CultureInfo? culture)
    {
        RequestCulture? cultures = culture is not null
            ? new(culture)
            : null;
        await UserPreferences.SetPreferredCulturesAsync(cultures);

        NavigationManager.Refresh(forceReload: true);
    }

    private static (string icon, string localizerKey) GetColorThemeInfo(ColorTheme theme)
    {
        string icon = theme == ColorTheme.Auto
            ? Outlined.Contrast
            : $"material-symbols-outlined/{theme}_mode".ToLowerInvariant();
        string key = $"theme_{theme}Mode";

        return (icon, key);
    }

    private string GetFlagImgUrl(CultureInfo culture) => Assets[$"/_content/flags/{culture.Name.Replace('-', '_')}.svg"];

    public void Dispose()
    {
        _stateSubscription?.Dispose();
    }
}