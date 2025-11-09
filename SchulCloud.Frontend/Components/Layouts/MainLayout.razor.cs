using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using SchulCloud.Identity.Enums;

namespace SchulCloud.Frontend.Components.Layouts;

public sealed partial class MainLayout : BaseLayout
{
    #region Injections
    [Inject]
    private IStringLocalizer<MainLayout> Localizer { get; set; } = null!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = null!;
    #endregion
    
    private ApplicationUser _user = null!;
    private bool _drawerOpen = false;
    
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = null!;

    [CascadingParameter]
    private Task<ApplicationUser> CurrentUser { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _user = await CurrentUser;
        await base.OnInitializedAsync();
    }

    protected override async Task<(ColorTheme, CultureInfo?)> GetUserPreferencesAsync()
    {
        AuthenticationState state = await AuthenticationState;
        ColorTheme theme = UserManager.GetColorTheme(state.User);
        
        return (theme, null);
    }

    private void ToggleMenu_Click()
    {
        _drawerOpen = !_drawerOpen;
    }
}
