using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MudBlazor;
using SchulCloud.Frontend.Options;
using SchulCloud.Identity.Enums;

namespace SchulCloud.Frontend.Components.Layouts;

public sealed partial class MainLayout : LayoutComponentBase
{
    #region Injections
    [Inject]
    private ILogger<MainLayout> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<MainLayout> Localizer { get; set; } = default!;

    [Inject]
    private IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;
    #endregion

    private MudThemeProvider _themeProvider = default!;

    private ApplicationUser _user = default!;
    private bool _drawerOpen = false;

    private bool _isAutoThemeMode;
    private bool _isDarkMode;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState;

        try
        {
            Logger.LogDebug("Wait for semaphore");
            Logger.LogDebug("Entered semaphore");
            _user = (await UserManager.GetUserAsync(state.User))!;
        }
        finally
        {
            Logger.LogDebug("Released semaphore");
        }

        ColorTheme theme = UserManager.GetColorTheme(state.User);
        _isAutoThemeMode = theme == ColorTheme.Auto;
        _isDarkMode = theme == ColorTheme.Dark;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _isAutoThemeMode)
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

    private void ToggleMenu_Click()
    {
        _drawerOpen = !_drawerOpen;
    }
}
