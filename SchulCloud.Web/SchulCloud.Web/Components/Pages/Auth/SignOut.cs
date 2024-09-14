using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Web.Components.Pages.Auth;

[AllowAnonymous]     // it doesn't have a .razor page so _Imports.razor won't be applied.
[Route("/auth/signOut")]
public sealed class SignOut : ComponentBase
{
    [Inject]
    private ILogger<SignOut> Logger { get; set; } = default!;

    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState;
        if (SignInManager.IsSignedIn(authenticationState.User))
        {
            if (HttpContext is null)
            {
                NavigationManager.Refresh(forceReload: true);     // Forces the client to send a GET request which is required for the sign out.
                return;
            }

            await SignInManager.SignOutAsync();

            string userId = UserManager.GetUserId(authenticationState.User)!;
            Logger.LogDebug("User {id} signed out.", userId);
        }

        NavigationManager.NavigateToSignIn();
    }
}
