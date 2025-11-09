using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using SchulCloud.Frontend.Options;
using SchulCloud.Frontend.Services.Interfaces;
using SchulCloud.Identity.Enums;

namespace SchulCloud.Frontend.Components.Layouts;

public abstract class BaseLayout : LayoutComponentBase, IDisposable
{
    #region Injections
    [Inject]
    protected IUserPreferencesStore UserPreferences { get; set; } = null!;

    [Inject]
    protected IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = null!;

    [Inject]
    protected IOptions<RequestLocalizationOptions> LocalizationOptionsAccessor { get; set; } = null!;
    
    [Inject]
    protected PersistentComponentState ComponentState { get; set; } = null!;
    #endregion
    
    protected RequestLocalizationOptions LocalizationOptions => LocalizationOptionsAccessor.Value;

    protected MudThemeProvider _themeProvider = null!;

    protected bool IsAutoColorTheme => _colorTheme == ColorTheme.Auto;
    protected bool _isDarkMode;

    protected ColorTheme _colorTheme;
    protected CultureInfo? _culture;

    protected PersistingComponentStateSubscription? _stateSubscription;

    [CascadingParameter]
    protected HttpContext? HttpContext { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext is not null)
        { 
            (_colorTheme, _culture) = await GetUserPreferencesAsync();
            if (_colorTheme == ColorTheme.Auto)
            {
                string? value = HttpContext.Request.Cookies[".AspNetCore.AutoDarkTheme"];
                _isDarkMode = !string.IsNullOrEmpty(value);     // Cookie used as flag to indicate whether dark mode ist prefered by the browser
            }
            
            _stateSubscription = ComponentState.RegisterOnPersisting(() =>
            {
                ComponentState.PersistAsJson(nameof(_isDarkMode), _isDarkMode);
                ComponentState.PersistAsJson(nameof(_colorTheme), _colorTheme);
                ComponentState.PersistAsJson(nameof(_culture), _culture?.ToString());

                return Task.CompletedTask;
            });
        }
        else
        {
            ComponentState.TryTakeFromJson(nameof(_isDarkMode), out _isDarkMode);
            ComponentState.TryTakeFromJson(nameof(_colorTheme), out _colorTheme);
            if (ComponentState.TryTakeFromJson(nameof(_culture), out string? culture))
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    _culture = CultureInfo.GetCultureInfo(culture!);
                }
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && IsAutoColorTheme)
        {
            _isDarkMode = await _themeProvider.GetSystemDarkModeAsync();
            StateHasChanged();

            await _themeProvider.WatchSystemDarkModeAsync(darkMode =>
            {
                _isDarkMode = darkMode;
                StateHasChanged();

                return Task.CompletedTask;
            });
        }
    }

    protected abstract Task<(ColorTheme, CultureInfo?)> GetUserPreferencesAsync();
    
    public virtual void Dispose()
    {
        _stateSubscription?.Dispose();
    }
}