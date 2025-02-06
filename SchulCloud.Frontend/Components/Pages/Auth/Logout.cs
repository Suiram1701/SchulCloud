using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Frontend.Components.Pages.Auth;

[AllowAnonymous]     // it doesn't have a .razor page so _Imports.razor won't be applied.
[Route("/auth/logout")]
[ExcludeFromInteractiveRouting]
public sealed class Logout : ComponentBase
{
    [Inject]
    private ILogger<Logout> Logger { get; set; } = default!;

    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState;
        if (SignInManager.IsSignedIn(authenticationState.User))
        {
            await SignInManager.SignOutAsync();

            string userId = UserManager.GetUserId(authenticationState.User)!;
            Logger.LogDebug("User {id} signed out.", userId);
        }

#if DEBUG
        try
        {
            NavigationManager.NavigateToLogin();
        }
        catch (NavigationException)
        {
            throw;     // This prevents the debugger from breaking at this point.
        }
#else
        NavigationManager.NavigateToLogin();
#endif
    }
}
