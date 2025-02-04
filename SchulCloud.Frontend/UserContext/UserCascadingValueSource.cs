using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SchulCloud.Frontend.Identity.Managers;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace SchulCloud.Frontend.UserContext;

/// <summary>
/// A cascading value source that provides the current user.
/// </summary>
public class UserCascadingValueSource : CascadingValueSource<Task<ApplicationUser?>>, IDisposable
{
    private readonly ApplicationUserManager _userManager;
    private readonly SchulCloudSignInManager _signInManager;
    private readonly AuthenticationStateProvider _stateProvider;

    private bool disposedValue;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="userManager">The user manager used to get the <see cref="ApplicationUser"/>.</param>
    /// <param name="signInManager">The sign in manager used to check whether the user is signed in.</param>
    /// <param name="stateProvider">The <see cref="AuthenticationStateProvider"/> used to get the <see cref="ClaimsPrincipal"/>.</param>
    public UserCascadingValueSource(ApplicationUserManager userManager, SchulCloudSignInManager signInManager, AuthenticationStateProvider stateProvider)
        : base(() => GetCurrentUserTask(userManager, signInManager, stateProvider.GetAuthenticationStateAsync()), isFixed: false)     // The factory is used because the GetAuthenticationStateAsync have to be called on component side
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _stateProvider = stateProvider;

        _stateProvider.AuthenticationStateChanged += AuthenticationStateChanged;
    }

    private void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        Task<ApplicationUser?> userTask = GetCurrentUserTask(_userManager, _signInManager, task);
        _ = NotifyChangedAsync(userTask);
    }

    private static async Task<ApplicationUser?> GetCurrentUserTask(ApplicationUserManager userManager, SchulCloudSignInManager signInManager, Task<AuthenticationState> stateTask)
    {
        AuthenticationState state = await stateTask;
        if (signInManager.IsSignedIn(state.User))
        {
            return await userManager.GetUserAsync(state.User);
        }
        else
        {
            return null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                _stateProvider.AuthenticationStateChanged -= AuthenticationStateChanged;
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
