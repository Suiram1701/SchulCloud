﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using SchulCloud.Database.Models;

namespace SchulCloud.Web.Components.Pages.Auth;

[AllowAnonymous]
[Route("/auth/signOut")]
public sealed class SignOut : ComponentBase
{
    [Inject]
    private ILogger<SignOut> Logger { get; set; } = default!;

    [Inject]
    private SignInManager<User> SignInManager { get; set; } = default!;

    [Inject]
    private UserManager<User> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState.ConfigureAwait(false);

        if (SignInManager.IsSignedIn(state.User))
        {
            if (HttpContext is null)
            {
                NavigationManager.Refresh(forceReload: true);     // Forces the client to send a GET request which is required for the sign out.
                return;
            }

            await SignInManager.SignOutAsync().ConfigureAwait(false);

            string userId = UserManager.GetUserId(state.User)!;
            Logger.LogDebug("User {id} signed out.", userId);
        }

        NavigationManager.NavigateToSignIn();
    }
}