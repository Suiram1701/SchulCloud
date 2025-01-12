using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Identity.Enums;
using System.Globalization;

namespace SchulCloud.Frontend.Components.Pages.Account;

[Route("/account/settings")]
public sealed partial class Settings : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<Settings> Localizer { get; set; } = default!;

    [Inject]
    private IOptions<RequestLocalizationOptions> LocalizationOptionsAccessor { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;

    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private RequestLocalizationOptions LocalizationOptions => LocalizationOptionsAccessor.Value;

    private ApplicationUser _user = default!;

    private bool _fromBrowserCulture;
    private CultureInfo? _culture;
    private CultureInfo? _uiCulture;

    private ColorTheme _theme;

    [SupplyParameterFromQuery(Name = "reload")]
    public bool? ReloadToken { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState;
        _user = (await UserManager.GetUserAsync(authenticationState.User))!;

        if (ReloadToken ?? false)
        {
            await RefreshSessionAsync();
            return;
        }

        _culture = await UserManager.GetCultureAsync(_user);
        _uiCulture = await UserManager.GetUiCultureAsync(_user);
        _fromBrowserCulture = _culture is null || _uiCulture is null;

        _theme = await UserManager.GetColorThemeAsync(_user);
    }

    private async Task OnBrowserCultureChangedAsync(bool newValue)
    {
        CultureInfo? newCulture = !newValue
            ? CultureInfo.CurrentUICulture
            : null;
        await UserManager.SetCultureAsync(_user, newCulture);
        await UserManager.SetUiCultureAsync(_user, newCulture);

        RefreshSettings();
    }

    private async Task OnCultureChangedAsync(CultureInfo? newCulture)
    {
        await UserManager.SetCultureAsync(_user, newCulture);
        RefreshSettings();
    }

    private async Task OnUiCultureChangedAsync(CultureInfo? newCulture)
    {
        await UserManager.SetUiCultureAsync(_user, newCulture);
        RefreshSettings();
    }

    private async Task OnColorThemeChangedAsync(ColorTheme theme)
    {
        await UserManager.SetColorThemeAsync(_user, theme);
        RefreshSettings();
    }

    private void RefreshSettings() => NavigationManager.NavigateToAccountSettings(reload: true, forceLoad: true);

    private async Task RefreshSessionAsync()
    {
        if (HttpContext is not null)
        {
            await SignInManager.RefreshSignInAsync(_user);
            NavigationManager.NavigateToAccountSettings(reload: null);
        }
        else
        {
            NavigationManager.Refresh(forceReload: true);
        }
    }
}
