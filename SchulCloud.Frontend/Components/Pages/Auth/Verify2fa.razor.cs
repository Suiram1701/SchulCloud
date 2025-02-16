using Fido2NetLib;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using SchulCloud.Frontend.Enums;
using SchulCloud.Frontend.Extensions;
using SchulCloud.Frontend.Identity.Managers;
using SchulCloud.Frontend.JsInterop;
using SchulCloud.Frontend.Models;
using SchulCloud.Frontend.Services.Exceptions;
using SchulCloud.Frontend.Services.Interfaces;
using System.Security.Cryptography;

namespace SchulCloud.Frontend.Components.Pages.Auth;

[Route("/auth/verify2fa")]
public sealed partial class Verify2fa : ComponentBase, IDisposable
{
    #region Injections
    [Inject]
    private IMemoryCache Cache { get; set; } = default!;

    [Inject]
    private IStringLocalizer<Verify2fa> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private Identity.EmailSenders.IEmailSender<ApplicationUser> EmailSender { get; set; } = default!;

    [Inject]
    private IRequestLimiter<ApplicationUser> Limiter { get; set; } = default!;

    [Inject]
    private AntiforgeryStateProvider AntiforgeryStateProvider { get; set; } = default!;

    [Inject]
    private SchulCloudSignInManager SignInManager { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private WebAuthnInterop WebAuthnService { get; set; } = default!;

    [Inject]
    private PersistentComponentState ComponentState { get; set; } = default!;
    #endregion

    private ElementReference _formRef = default!;
    private const string _formName = "verify2fa";

    private ApplicationUser _user = default!;
    private bool _mfaEmailEnabled;
    private bool _mfaSecurityKeyEnabled;

    private string? _errorMessage;
    private PersistingComponentStateSubscription? _persistingSubscription;

    private bool _webAuthnSupported = true;
    private readonly CancellationTokenSource _webAuthnCts = new();

    private bool IsInvalid => _errorMessage is not null;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery(Name = "persistent")]
    public bool Persistent { get; set; }

    [SupplyParameterFromForm(FormName = _formName)]
    public Verify2faModel Model { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        if (!UserManager.SupportsUserTwoFactor)
        {
            NavigationManager.NavigateToLogin(returnUrl: ReturnUrl);
            return;
        }

        // Make sure that a valid antiforgery token is available.
        if (AntiforgeryStateProvider.GetAntiforgeryToken() is null)
        {
            NavigationManager.Refresh(forceReload: true);
        }

        if (ComponentState.TryTakeFromJson(nameof(_user), out ApplicationUser? user))
        {
            _user = user!;
        }
        ComponentState.TryTakeFromJson(nameof(_mfaEmailEnabled), out _mfaEmailEnabled);
        ComponentState.TryTakeFromJson(nameof(_mfaSecurityKeyEnabled), out _mfaSecurityKeyEnabled);

        if (HttpContext is not null)
        {
            user ??= await SignInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is not null)
            {
                _user = user!;

                _mfaEmailEnabled = UserManager.SupportsUserTwoFactorEmail
                    && await UserManager.GetTwoFactorEmailEnabledAsync(_user);
                _mfaSecurityKeyEnabled = UserManager.SupportsUserTwoFactorSecurityKeys
                    && await UserManager.GetTwoFactorSecurityKeyEnableAsync(_user);

                _persistingSubscription = ComponentState.RegisterOnPersisting(() =>
                {
                    ComponentState.PersistAsJson(nameof(_user), _user);
                    ComponentState.PersistAsJson(nameof(_mfaEmailEnabled), _mfaEmailEnabled);
                    ComponentState.PersistAsJson(nameof(_mfaSecurityKeyEnabled), _mfaSecurityKeyEnabled);

                    return Task.CompletedTask;
                }, RenderMode.InteractiveServer);
            }
            else
            {
                NavigationManager.NavigateToLogin(returnUrl: ReturnUrl, forceLoad: true);
                return;
            }

            if (HttpMethods.IsPost(HttpContext.Request.Method))
            {
                await Verify2faAsync();

                _persistingSubscription = ComponentState.RegisterOnPersisting(() =>
                {
                    ComponentState.PersistAsJson(nameof(Model), Model);
                    ComponentState.PersistAsJson(nameof(_errorMessage), _errorMessage);

                    return Task.CompletedTask;
                }, RenderMode.InteractiveServer);
            }
        }
        else
        {
            if (ComponentState.TryTakeFromJson(nameof(Model), out Verify2faModel? persistedModel))
            {
                Model = persistedModel!;
            }
            ComponentState.TryTakeFromJson(nameof(_errorMessage), out _errorMessage);
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

    private async Task SendEmailAuthenticationCode_ClickAsync()
    {
        if (!UserManager.SupportsUserTwoFactorEmail || !_mfaEmailEnabled)
        {
            return;
        }

        if (!await Limiter.CanRequestTwoFactorEmailCodeAsync(_user))
        {
            DateTimeOffset? expiration = await Limiter.GetTwoFactorEmailCodeTimeoutAsync(_user);

            SnackbarService.AddInfo(Localizer["emailConfirmation_Timeout", expiration.Humanize()]);
        }
        else
        {
            // Renew the user instance because a persistent instance isn't changed tracked anymore.
            string userId = await UserManager.GetUserIdAsync(_user);
            ApplicationUser user = (await UserManager.FindByIdAsync(userId))!;

            string code = await UserManager.GenerateTwoFactorEmailCodeAsync(user);
            if (string.IsNullOrEmpty(code))
            {
                throw new Exception("An unknown error occurred during the email code generation.");
            }

            // Show the Toast before the email is sent for better user experience (sending the email is sometimes time expensive).
            string anonymizedAddress = await UserManager.GetAnonymizedEmailAsync(user);
            SnackbarService.AddInfo(Localizer["emailConfirmation", anonymizedAddress]);

            string email = (await UserManager.GetEmailAsync(user))!;
            await EmailSender.Send2faEmailCodeAsync(user, email, code);
        }
    }

    private async Task SecurityKeyAuthentication_ClickAsync()
    {
        if (!UserManager.SupportsUserTwoFactorSecurityKeys || !_webAuthnSupported)
        {
            return;
        }

        AssertionOptions assertionOptions = await UserManager.CreateFido2AssertionOptionsAsync(null);

        try
        {
            AuthenticatorAssertionRawResponse authenticatorResponse = await WebAuthnService.GetCredentialAsync(assertionOptions, _webAuthnCts.Token);

            string key = RandomNumberGenerator.GetHexString(32);
            Model.AuthenticatorDataAccessKey = key;

            string cacheKey = await GetSecurityKeyDataCacheKeyAsync(key);


            ICacheEntry entry = Cache.CreateEntry(cacheKey)
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetValue(new SecurityKeyAuthState(assertionOptions, authenticatorResponse))
                .SetSize(101);     // Calculated by adding properties sizes of options and response together using a integrated security key.
            entry.Dispose();

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

    private async Task Verify2faAsync()
    {
        // Verifies that the selected verification method is supported (manipulation possible through form field)
        switch (Model.Method)
        {
            case TwoFactorMethod.Email when !UserManager.SupportsUserTwoFactorEmail:
            case TwoFactorMethod.SecurityKey when !UserManager.SupportsUserTwoFactorSecurityKeys:
            case TwoFactorMethod.Recovery when !UserManager.SupportsUserTwoFactorRecoveryCodes:
                return;
        }

        SignInResult verificationResult = await ProcessVerification(Model.Method);
        switch (verificationResult)
        {
            case { Succeeded: true }:
                NavigationManager.NavigateSaveTo(ReturnUrl ?? Routes.Dashboard());
                break;
            case { IsLockedOut: true }:
                DateTimeOffset lockOutEnd = (await UserManager.GetLockoutEndDateAsync(_user)).Value;

                _errorMessage = lockOutEnd.Offset <= TimeSpan.MaxValue     // MaxValue means the user is locked without an end. It has to be unlocked manually.
                    ? Localizer["signIn_LockedOut", lockOutEnd.Humanize()]
                    : Localizer["signIn_LockedOut_NotSpecified"];
                break;
            default:
                _errorMessage = Localizer["signIn_" + verificationResult];
                break;
        }
    }

    private async Task<SignInResult> ProcessVerification(TwoFactorMethod method) => method switch
    {
        TwoFactorMethod.Authenticator => await SignInManager.TwoFactorAuthenticatorSignInAsync(Model.TrimmedCode, Persistent, Model.ShouldRememberClient),
        TwoFactorMethod.Email => await SignInManager.TwoFactorEmailSignInAsync(Model.TrimmedCode, Persistent, Model.ShouldRememberClient),
        TwoFactorMethod.SecurityKey => await VerifyTwoFactorSecurityKeyAsync(Model.AuthenticatorDataAccessKey, Persistent, Model.ShouldRememberClient),
        TwoFactorMethod.Recovery => await SignInManager.TwoFactorRecoveryCodeSignInAsync(Model.TrimmedCode),
        _ => SignInResult.Failed
    };

    private async Task<SignInResult> VerifyTwoFactorSecurityKeyAsync(string? dataAccessKey, bool isPersistent, bool rememberClient)
    {
        if (string.IsNullOrWhiteSpace(dataAccessKey))
        {
            return SignInResult.Failed;
        }

        string cacheKey = await GetSecurityKeyDataCacheKeyAsync(dataAccessKey);
        if (!Cache.TryGetValue(cacheKey, out SecurityKeyAuthState? authState))
        {
            return SignInResult.Failed;
        }

        Cache.Remove(dataAccessKey!);
        return await SignInManager.TwoFactorFido2UserCredentialSignInAsync(authState!.Options, authState.Response, isPersistent, rememberClient);
    }

    private async Task<string> GetSecurityKeyDataCacheKeyAsync(string key)
    {
        string userId = await UserManager.GetUserIdAsync(_user);
        return $"twoFactor_securityKeyData_{userId}_{key}";
    }

    public void Dispose()
    {
        _webAuthnCts.Cancel();

        _webAuthnCts.Dispose();
        _persistingSubscription?.Dispose();
    }

    private record SecurityKeyAuthState(AssertionOptions Options, AuthenticatorAssertionRawResponse Response);
}
