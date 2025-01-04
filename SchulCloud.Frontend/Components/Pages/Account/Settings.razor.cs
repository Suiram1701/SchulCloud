using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
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
    private CultureInfo? _culture;
    private CultureInfo? _uiCulture;

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
            if (HttpContext is not null)
            {
                await SignInManager.RefreshSignInAsync(_user);
                NavigationManager.NavigateToAccountSettings(reload: false);
            }
            else
            {
                NavigationManager.Refresh(forceReload: true);
            }

            return;
        }

        _culture = await UserManager.GetCultureAsync(_user);
        _uiCulture = await UserManager.GetUiCultureAsync(_user);
    }

    private async Task OnCultureChangedAsync(CultureInfo? newCulture)
    {
        await UserManager.SetCultureAsync(_user, newCulture);
        NavigationManager.NavigateToAccountSettings(reload: true, forceLoad: true);
    }

    private async Task OnUiCultureChangedAsync(CultureInfo? newCulture)
    {
        await UserManager.SetUiCultureAsync(_user, newCulture);
        NavigationManager.NavigateToAccountSettings(reload: true, forceLoad: true);
    }
}
