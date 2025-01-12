using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MudBlazor;
using SchulCloud.Frontend.Options;
using SchulCloud.Identity.Enums;
using System.Security.Claims;

namespace SchulCloud.Frontend.Components.Layouts;

public sealed partial class MainLayout : LayoutComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<MainLayout> Localizer { get; set; } = default!;

    [Inject]
    private IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;
    #endregion

    private MudThemeProvider _themeProvider = default!;

    private ClaimsPrincipal _user = default!;
    private bool _drawerOpen = false;

    private bool _isAutoThemeMode;
    private bool _isDarkMode;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState;
        _user = state.User;

        ColorTheme theme = UserManager.GetColorTheme(_user);
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

    private static string NameToDisplayedAvatar(string username) => string.Concat(username.Split(' ', 2).Select(part => part.First()));
}
