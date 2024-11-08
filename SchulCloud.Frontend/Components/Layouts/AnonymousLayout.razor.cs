using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Frontend.Enums;
using SchulCloud.Frontend.Options;
using SchulCloud.Frontend.Services.Interfaces;
using System.Globalization;

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

    private bool _isDarkMode;
    private CultureInfo? _culture;

    private PersistingComponentStateSubscription? _stateSubscription;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext is not null)
        {
            ColorTheme colorTheme = await UserPreferences.GetPreferredColorThemeAsync();
            _isDarkMode = colorTheme == ColorTheme.Dark;

            RequestCulture? cultures = await UserPreferences.GetPreferredCulturesAsync();
            _culture = cultures?.UICulture;

            _stateSubscription = ComponentState.RegisterOnPersisting(() =>
            {
                ComponentState.PersistAsJson(nameof(_isDarkMode), _isDarkMode);
                ComponentState.PersistAsJson(nameof(_culture), _culture?.ToString());

                return Task.CompletedTask;
            });
        }
        else
        {
            ComponentState.TryTakeFromJson(nameof(_isDarkMode), out _isDarkMode);
            if (ComponentState.TryTakeFromJson(nameof(_culture), out string? culture))
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    _culture = CultureInfo.GetCultureInfo(culture!);
                }
            }
        }
    }

    private async Task IsDarkMode_ChangedAsync()
    {
        _isDarkMode = !_isDarkMode;

        ColorTheme theme = _isDarkMode
            ? ColorTheme.Dark
            : ColorTheme.Light;
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

    public void Dispose()
    {
        _stateSubscription?.Dispose();
    }
}
