using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Database.Models;
using SchulCloud.Web.Options;
using System.Security.Claims;

namespace SchulCloud.Web.Components.Layouts;

[Authorize]
public partial class MainLayout : LayoutComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<MainLayout> Localizer { get; set; } = default!;

    [Inject]
    private IOptionsSnapshot<PresentationOptions> PresentationOptions { get; set; } = default!;

    [Inject]
    private UserManager<User> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private ClaimsPrincipal _userPrincipial = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState.ConfigureAwait(false);
        _userPrincipial = state.User;
    }

    private bool IsActive(string path)
    {
        Uri uri = new(NavigationManager.Uri);
        return uri.AbsolutePath.Equals(path);
    }
}
