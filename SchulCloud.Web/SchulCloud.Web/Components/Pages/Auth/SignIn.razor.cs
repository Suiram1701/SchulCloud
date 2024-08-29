using BlazorBootstrap;
using Fido2NetLib;
using Humanizer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using SchulCloud.Store.Managers;
using SchulCloud.Web.Constants;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Identity.Managers;
using SchulCloud.Web.Models;
using SchulCloud.Web.Services;
using SchulCloud.Web.Services.EventArgs;
using System.Security.Cryptography;

namespace SchulCloud.Web.Components.Pages.Auth;

[Route("/auth/signIn")]
public sealed partial class SignIn : ComponentBase, IAsyncDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<SignIn> Localizer { get; set; } = default!;

    [Inject]
    private IMemoryCache Cache { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private AntiforgeryStateProvider AntiforgeryStateProvider { get; set; } = default!;

    [Inject]
    private SchulCloudUserManager<ApplicationUser, AppCredential> UserManager { get; set; } = default!;

    [Inject]
    private SchulCloudSignInManager<ApplicationUser, AppCredential> SignInManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private WebAuthnService WebAuthnService { get; set; } = default!;

    [Inject]
    private PersistentComponentState ComponentState { get; set; } = default!;
    #endregion

    private const string _formName = "signIn";

    private ElementReference _formRef = default!;

    private string? _errorMessage;
    private PersistingComponentStateSubscription? _persistingSubscription;

    private bool _webAuthnSupported = true;
    private AssertionOptions? _assertionOptions;
    private IAsyncDisposable? _pendingAssertion;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        if (!await WebAuthnService.IsSupportedAsync().ConfigureAwait(false))
        {
            _webAuthnSupported = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private async Task SignInAsync()
    {
        SignInResult signInResult;
        ApplicationUser? user;
        if (string.IsNullOrWhiteSpace(Model.AuthenticatorDataAccessKey))
        {
            user = await UserManager.FindByEmailAsync(Model.User).ConfigureAwait(false);
            user ??= await UserManager.FindByNameAsync(Model.User).ConfigureAwait(false);
            if (user is null)
            {
                _errorMessage = Localizer["signIn_" + SignInResult.Failed];
                return;
            }

            signInResult = await SignInManager.PasswordSignInAsync(user, Model.Password, Model.Persistent, lockoutOnFailure: true).ConfigureAwait(false);
        }
        else
        {
            (signInResult, user) = await SecurityKeySignInAsync(Model.AuthenticatorDataAccessKey, Model.Persistent).ConfigureAwait(false);
            Model.AuthenticatorDataAccessKey = null;
        }

        switch (signInResult)
        {
            case { Succeeded: true }:
                Uri returnUri = NavigationManager.ToAbsoluteUri(ReturnUrl);
                NavigationManager.NavigateTo(returnUri.PathAndQuery);     // prevent a redirect to another domain by using only the path and query part.
                break;
            case { RequiresTwoFactor: true }:
                NavigationManager.NavigateToVerify2fa(persistent: Model.Persistent, returnUrl: ReturnUrl, forceLoad: true);
                break;
            case { IsLockedOut: true }:
                DateTimeOffset lockOutEnd = (await UserManager.GetLockoutEndDateAsync(user!).ConfigureAwait(false)).Value;

                _errorMessage = lockOutEnd.Offset < TimeSpan.MaxValue     // MaxValue means that the user is locked without an end. It has to unlocked manually.
                    ? Localizer["signIn_LockedOut", lockOutEnd.Humanize()]
                    : Localizer["signIn_LockedOut_NotSpecified"];
                break;
            default:
                _errorMessage = Localizer["signIn_" + signInResult];
                break;
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

    private async Task StartPasskeySignIn_ClickAsync()
    {
        if (!_webAuthnSupported)
        {
            return;
        }

        _assertionOptions = await UserManager.CreateFido2AssertionOptionsAsync(null).ConfigureAwait(false);
        _pendingAssertion = await WebAuthnService.StartGetCredentialAsync(_assertionOptions, OnGetCredentialCompletedCallback).ConfigureAwait(false);
    }

    private async void OnGetCredentialCompletedCallback(object? sender, WebAuthnCompletedEventArgs<AuthenticatorAssertionRawResponse> args)
    {
        if (args.Successful)
        {
            string key = RandomNumberGenerator.GetHexString(32);
            Model.AuthenticatorDataAccessKey = key;
            StateHasChanged();

            string cacheKey = GetSecurityKeyDataCacheKey(key);
            using (ICacheEntry entry = Cache.CreateEntry(cacheKey))
            {
                entry.SetValue(new SecurityKeyAuthState(_assertionOptions!, args.Result));
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            try
            {
                await JSRuntime.FormSubmitAsync(_formRef).ConfigureAwait(false);
            }
            catch (JSDisconnectedException) { }
        }
        else
        {
            ToastService.NotifyError(Localizer["securityKey_Error"], args.ErrorMessage ?? Localizer["securityKey_ErrorMessage"]);
        }
    }

    private async Task<(SignInResult result, ApplicationUser? user)> SecurityKeySignInAsync(string? dataAccessKey, bool isPersistent)
    {
        if (string.IsNullOrWhiteSpace(dataAccessKey))
        {
            return (SignInResult.Failed, null);
        }

        string cacheKey = GetSecurityKeyDataCacheKey(dataAccessKey);
        if (!Cache.TryGetValue(cacheKey, out SecurityKeyAuthState? authState))
        {
            return (SignInResult.Failed, null);
        }

        Cache.Remove(dataAccessKey!);
        return await SignInManager.Fido2PasskeySignInAsync(authState!.Options, authState.Response, isPersistent).ConfigureAwait(false);
    }

    private static string GetSecurityKeyDataCacheKey(string key)
    {
        return $"signIn_securityKeyData_{key}";
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

    public async ValueTask DisposeAsync()
    {
        if (_pendingAssertion is not null)
        {
            await _pendingAssertion.DisposeAsync().ConfigureAwait(false);
        }

        _persistingSubscription?.Dispose();
    }

    private record SecurityKeyAuthState(AssertionOptions Options, AuthenticatorAssertionRawResponse Response);
}