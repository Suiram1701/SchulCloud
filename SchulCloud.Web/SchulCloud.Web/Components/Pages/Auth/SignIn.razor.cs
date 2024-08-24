using Humanizer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SchulCloud.Web.Constants;
using SchulCloud.Web.Models;

namespace SchulCloud.Web.Components.Pages.Auth;

[Route("/auth/signIn")]
public sealed partial class SignIn : ComponentBase, IDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<SignIn> Localizer { get; set; } = default!;

    [Inject]
    private AntiforgeryStateProvider AntiforgeryStateProvider { get; set; } = default!;

    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private PersistentComponentState ComponentState { get; set; } = default!;
    #endregion

    private const string _formName = "signIn";

    private string? _errorMessage;
    private PersistingComponentStateSubscription? _persistingSubscription;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromForm(FormName = _formName)]
    public SignInModel Model { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        // Make sure that a valid antiforgery token is available. 
        if (AntiforgeryStateProvider.GetAntiforgeryToken() is null)
        {
            NavigationManager.Refresh(forceReload: true);
        }

        if (HttpContext is not null)
        {
            // Make sure that every auth cookie is cleaned up if present.
            AuthenticationState state = await AuthenticationState.ConfigureAwait(false);
            if (SignInManager.IsSignedIn(state.User) || (await HttpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme).ConfigureAwait(false)).Principal is not null)
            {
                await SignInManager.SignOutAsync().ConfigureAwait(false);
            }

            if (HttpMethods.IsPost(HttpContext.Request.Method))
            {
                await SignInAsync().ConfigureAwait(false);

                _persistingSubscription = ComponentState.RegisterOnPersisting(() =>
                {
                    ComponentState.PersistAsJson(nameof(Model), Model);
                    ComponentState.PersistAsJson(nameof(_errorMessage), _errorMessage);

                    return Task.CompletedTask;
                });
            }
        }
        else
        {
            if (ComponentState.TryTakeFromJson(nameof(Model), out SignInModel? persistedModel))
            {
                Model = persistedModel!;
            }
            ComponentState.TryTakeFromJson(nameof(_errorMessage), out _errorMessage);
        }
    }

    private async Task ForgotPasswordAsync_ClickAsync()
    {
        ApplicationUser? user = await UserManager.FindByEmailAsync(Model.User).ConfigureAwait(false);
        user ??= await UserManager.FindByNameAsync(Model.User).ConfigureAwait(false);

        string? userId = user is not null
            ? await UserManager.GetUserIdAsync(user).ConfigureAwait(false)
            : null;

        string resetUrl = Routes.ResetPassword(userId: userId, returnUrl: ReturnUrl);
        NavigationManager.NavigateTo(resetUrl);
    }

    private async Task SignInAsync()
    {
        ApplicationUser? user = await UserManager.FindByEmailAsync(Model.User).ConfigureAwait(false);
        user ??= await UserManager.FindByNameAsync(Model.User).ConfigureAwait(false);

        if (user is null)
        {
            _errorMessage = Localizer["signIn_" + SignInResult.Failed];
            return;
        }

        SignInResult result = await SignInManager.PasswordSignInAsync(user, Model.Password, Model.Persistent, lockoutOnFailure: true).ConfigureAwait(false);
        switch (result)
        {
            case { Succeeded: true }:
                Uri returnUri = NavigationManager.ToAbsoluteUri(ReturnUrl);
                NavigationManager.NavigateTo(returnUri.PathAndQuery);     // prevent a redirect to another domain by using only the path and query part.
                break;
            case { RequiresTwoFactor: true }:
                NavigationManager.NavigateToVerify2fa(persistent: Model.Persistent, returnUrl: ReturnUrl, forceLoad: true);
                break;
            case { IsLockedOut: true }:
                DateTimeOffset lockOutEnd = (await UserManager.GetLockoutEndDateAsync(user).ConfigureAwait(false)).Value;

                _errorMessage = lockOutEnd.UtcDateTime <= DateTime.MaxValue     // MaxValue means that the user is locked without an end. It has to unlocked manually.
                    ? Localizer["signIn_LockedOut", lockOutEnd.Humanize()]
                    : Localizer["signIn_LockedOut_NotSpecified"];
                break;
            default:
                _errorMessage = Localizer["signIn_" + result];
                break;
        }
    }

    private void Input_Changed()
    {
        _errorMessage = null;
    }

    private bool IsInvalid() => _errorMessage is not null;

    private string IsInvalidFormClass => IsInvalid()
        ? "form-invalid"
        : string.Empty;

    private string IsInvalidInputClass => IsInvalid()
        ? ExtendedBootstrapClass.IsInvalid
        : string.Empty;

    void IDisposable.Dispose()
    {
        _persistingSubscription?.Dispose();
    }
}
