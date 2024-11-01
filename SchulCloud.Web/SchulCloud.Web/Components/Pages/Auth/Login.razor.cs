using Fido2NetLib;
using Humanizer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using SchulCloud.Store.Managers;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Identity.Managers;
using SchulCloud.Web.Models;
using SchulCloud.Web.Services;
using SchulCloud.Web.Services.Exceptions;
using System.Security.Cryptography;

namespace SchulCloud.Web.Components.Pages.Auth;

[Route("/auth/login")]
public sealed partial class Login : ComponentBase, IDisposable
{
    #region Injections
    [Inject]
    private IMemoryCache Cache { get; set; } = default!;

    [Inject]
    private IStringLocalizer<Login> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private AntiforgeryStateProvider AntiforgeryStateProvider { get; set; } = default!;

    [Inject]
    private AppUserManager UserManager { get; set; } = default!;

    [Inject]
    private SchulCloudSignInManager SignInManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private WebAuthnService WebAuthnService { get; set; } = default!;

    [Inject]
    private PersistentComponentState ComponentState { get; set; } = default!;
    #endregion

    private ElementReference _formRef = default!;
    private const string _formName = "loginForm";

    private string? _errorMessage;
    private PersistingComponentStateSubscription? _persistingSubscription;

    private bool _webAuthnSupported = true;
    private readonly CancellationTokenSource _webAuthnCts = new();

    private bool _emailConfirmDialogVisible;
    private string? _emailConfirmUserId;

    private bool IsInvalid => _errorMessage is not null;

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
            AuthenticationState state = await AuthenticationState;
            if (SignInManager.IsSignedIn(state.User) || (await HttpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme)).Principal is not null)
            {
                await SignInManager.SignOutAsync();
            }

            if (HttpMethods.IsPost(HttpContext.Request.Method))
            {
                bool succeeded = await SignInAsync();
                if (!succeeded)
                {
                    // Persist state from initial HTTP request to interactivity begin.
                    _persistingSubscription = ComponentState.RegisterOnPersisting(() =>
                    {
                        ComponentState.PersistAsJson(nameof(Model), Model);
                        ComponentState.PersistAsJson(nameof(_errorMessage), _errorMessage);
                        ComponentState.PersistAsJson(nameof(_emailConfirmDialogVisible), _emailConfirmDialogVisible);
                        ComponentState.PersistAsJson(nameof(_emailConfirmUserId), _emailConfirmUserId);

                        return Task.CompletedTask;
                    });
                }
            }
        }
        else
        {
            if (ComponentState.TryTakeFromJson(nameof(Model), out SignInModel? persistedModel))
            {
                Model = persistedModel!;
            }
            ComponentState.TryTakeFromJson(nameof(_errorMessage), out _errorMessage);
            ComponentState.TryTakeFromJson(nameof(_emailConfirmDialogVisible), out _emailConfirmDialogVisible);
            ComponentState.TryTakeFromJson(nameof(_emailConfirmUserId), out _emailConfirmUserId);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (!await WebAuthnService.IsSupportedAsync())
            {
                _webAuthnSupported = false;
                StateHasChanged();
            }
        }
    }

    private void Input_Changed()
    {
        _errorMessage = null;
    }

    private async Task ForgotPasswordAsync_ClickAsync()
    {
        ApplicationUser? user = await UserManager.FindByEmailAsync(Model.User);
        user ??= await UserManager.FindByNameAsync(Model.User);

        string? userId = user is not null
            ? await UserManager.GetUserIdAsync(user)
            : null;

        string resetUrl = Routes.ResetPassword(userId: userId, returnUrl: ReturnUrl);
        NavigationManager.NavigateTo(resetUrl);
    }

    private async Task PasskeySignIn_ClickAsync()
    {
        if (!UserManager.SupportsUserPasskeys || !_webAuthnSupported)
        {
            return;
        }

        AssertionOptions assertionOptions = await UserManager.CreateFido2AssertionOptionsAsync(null);

        try
        {
            AuthenticatorAssertionRawResponse authenticatorResponse = await WebAuthnService.GetCredentialAsync(assertionOptions, _webAuthnCts.Token);

            string key = RandomNumberGenerator.GetHexString(32);
            Model.AuthenticatorDataAccessKey = key;

            string cacheKey = GetSecurityKeyDataCacheKey(key);
            using (ICacheEntry entry = Cache.CreateEntry(cacheKey))
            {
                entry.SetValue(new SecurityKeyAuthState(assertionOptions, authenticatorResponse));
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            StateHasChanged();
            await JSRuntime.FormSubmitAsync(_formRef);
        }
        catch (WebAuthnException ex)
        {
            SnackbarService.AddError(ex.Message ?? Localizer["webAuthn_Error"]);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task<bool> SignInAsync()
    {
        ApplicationUser? user = null;
        SignInResult signInResult = SignInResult.Failed;
        if (UserManager.SupportsUserPassword && string.IsNullOrWhiteSpace(Model.AuthenticatorDataAccessKey) && !string.IsNullOrEmpty(Model.User))
        {
            user = await UserManager.FindByEmailAsync(Model.User);
            user ??= await UserManager.FindByNameAsync(Model.User);
            if (user is null)
            {
                _errorMessage = Localizer["signIn_Failed"];
                return false;
            }

            signInResult = await SignInManager.PasswordSignInAsync(user, Model.Password, Model.IsPersistent, lockoutOnFailure: true);
        }
        else if (UserManager.SupportsUserPasskeys)
        {
            (signInResult, user) = await SecurityKeySignInAsync(Model.AuthenticatorDataAccessKey, Model.IsPersistent);
            Model.AuthenticatorDataAccessKey = null;
        }

        switch (signInResult)
        {
            case { Succeeded: true }:
                NavigationManager.NavigateSaveTo(ReturnUrl ?? Routes.Dashboard());
                break;
            case { RequiresTwoFactor: true }:
                NavigationManager.NavigateToVerify2fa(persistent: Model.IsPersistent, returnUrl: ReturnUrl, forceLoad: true);
                break;
            case { IsLockedOut: true }:
                DateTimeOffset lockOutEnd = (await UserManager.GetLockoutEndDateAsync(user!)).Value;

                _errorMessage = lockOutEnd < DateTimeOffset.MaxValue     // MaxValue means that the user is locked without an end. It has to unlocked manually.
                    ? Localizer["signIn_LockedOut", lockOutEnd.Humanize()]
                    : Localizer["signIn_LockedOut_NotSpecified"];
                break;
            case { IsNotAllowed: true }:
                if (!await UserManager.IsEmailConfirmedAsync(user!))
                {
                    _emailConfirmDialogVisible = true;
                    _emailConfirmUserId = await UserManager.GetUserIdAsync(user!);
                }
                else
                {
                    _errorMessage = Localizer["signIn_NotAllowed"];
                }
                break;
            default:
                _errorMessage = Localizer["signIn_Error"];
                break;
        }

        return signInResult.Succeeded;
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
        return await SignInManager.Fido2PasskeySignInAsync(authState!.Options, authState.Response, isPersistent);
    }

    private static string GetSecurityKeyDataCacheKey(string key)
    {
        return $"signIn_securityKeyData_{key}";
    }

    public void Dispose()
    {
        _webAuthnCts.Cancel();

        _webAuthnCts.Dispose();
        _persistingSubscription?.Dispose();
    }

    private record SecurityKeyAuthState(AssertionOptions Options, AuthenticatorAssertionRawResponse Response);
}