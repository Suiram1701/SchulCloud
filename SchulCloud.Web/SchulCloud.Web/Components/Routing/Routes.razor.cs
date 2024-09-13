using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using SchulCloud.Web.Components.Layouts;

namespace SchulCloud.Web.Components.Routing;

public sealed partial class Routes : ComponentBase
{
    #region Injections
    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    #endregion

    private Type LayoutType => _isAuthenticated
        ? typeof(MainLayout)
        : typeof(AnonymousLayout);

    private bool _isAuthenticated = false;

    [CascadingParameter]
    public Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState.ConfigureAwait(false);
        _isAuthenticated = SignInManager.IsSignedIn(state.User);
    }
}