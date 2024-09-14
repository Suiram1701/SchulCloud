using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Web.Options;
using System.Security.Claims;

namespace SchulCloud.Web.Components.Layouts;

public sealed partial class MainLayout : LayoutComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<MainLayout> Localizer { get; set; } = default!;

    [Inject]
    private IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    #endregion

    private ClaimsPrincipal _userPrincipial = default!;
    private bool _drawerOpen = false;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState;
        _userPrincipial = state.User;
    }

    private void ToggleMenu_Click()
    {
        _drawerOpen = !_drawerOpen;
    }

    private static string NameToDisplayedAvatar(string username) => string.Concat(username.Split(' ', 2).Select(part => part.First()));
}
