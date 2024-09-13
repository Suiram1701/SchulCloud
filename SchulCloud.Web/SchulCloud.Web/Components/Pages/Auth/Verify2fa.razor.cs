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
using SchulCloud.Store.Managers;
using SchulCloud.Web.Enums;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Identity.Managers;
using SchulCloud.Web.Models;
using SchulCloud.Web.Services;
using SchulCloud.Web.Services.EventArgs;
using SchulCloud.Web.Services.Interfaces;
using System.Security.Cryptography;

namespace SchulCloud.Web.Components.Pages.Auth;

[Route("/auth/verify2fa")]
public sealed partial class Verify2fa : ComponentBase, IAsyncDisposable
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
    private SchulCloudSignInManager<ApplicationUser, AppCredential> SignInManager { get; set; } = default!;

    [Inject]
    private SchulCloudUserManager<ApplicationUser, AppCredential> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private WebAuthnService WebAuthnService { get; set; } = default!;

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
    private AssertionOptions? _assertionOptions;
    private IAsyncDisposable? _pendingAssertion;

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
            user ??= await SignInManager.GetTwoFactorAuthenticationUserAsync().ConfigureAwait(false);
            if (user is not null)
            {
                _user = user!;
                _mfaEmailEnabled = await UserManager.GetTwoFactorEmailEnabledAsync(_user).ConfigureAwait(false);
                _mfaSecurityKeyEnabled = await UserManager.GetTwoFactorSecurityKeyEnableAsync(_user).ConfigureAwait(false);

                ComponentState.RegisterOnPersisting(() =>
                {
                    ComponentState.PersistAsJson(nameof(_user), _user);
                    ComponentState.PersistAsJson(nameof(_mfaEmailEnabled), _mfaEmailEnabled);
                    ComponentState.PersistAsJson(nameof(_mfaSecurityKeyEnabled), _mfaSecurityKeyEnabled);

                    return Task.CompletedTask;
                }, RenderMode.InteractiveServer);
            }
            else
            {
                NavigationManager.NavigateToSignIn(returnUrl: ReturnUrl, forceLoad: true);
                return;
            }

            if (HttpMethods.IsPost(HttpContext.Request.Method))
            {
                await Verify2faAsync().ConfigureAwait(false);

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
            if (!await WebAuthnService.IsSupportedAsync().ConfigureAwait(false))
            {
                _webAuthnSupported = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }
    }

    private void Input_Changed()
    {
        _errorMessage = null;
    }

    private async Task SendEmailAuthenticationCode_ClickAsync()
    {
        if (!_mfaEmailEnabled)
        {
            return;
        }

        if (!await Limiter.CanRequestTwoFactorEmailCodeAsync(_user).ConfigureAwait(false))
        {
            DateTimeOffset? expiration = await Limiter.GetTwoFactorEmailCodeExpirationTimeAsync(_user).ConfigureAwait(false);

            SnackbarService.AddInfo(Localizer["emailConfirmation_Timeout", expiration.Humanize()]);
        }
        else
        {
            // Renew the user instance because a persistent instance isn't changed tracked anymore.
            string userId = await UserManager.GetUserIdAsync(_user).ConfigureAwait(false);
            ApplicationUser user = (await UserManager.FindByIdAsync(userId).ConfigureAwait(false))!;

            string code = await UserManager.GenerateTwoFactorEmailCodeAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(code))
            {
                throw new Exception("An unknown error occurred during the email code generation.");
            }

            // Show the Toast before the email is sent for better user experience (sending the mail is time expensive).
            string anonymizedAddress = await UserManager.GetAnonymizedEmailAsync(user).ConfigureAwait(false);
            SnackbarService.AddInfo(Localizer["emailConfirmation", anonymizedAddress]);

            string email = (await UserManager.GetEmailAsync(user).ConfigureAwait(false))!;
            await EmailSender.Send2faEmailCodeAsync(user, email, code).ConfigureAwait(false);
        }
    }

    private async Task StartSecurityKeyAuthentication_ClickAsync()
    {
        if (!_webAuthnSupported)
        {
            return;
        }

        _assertionOptions = await UserManager.CreateFido2AssertionOptionsAsync(_user).ConfigureAwait(false);
        _pendingAssertion = await WebAuthnService.StartGetCredentialAsync(_assertionOptions, OnGetCredentialCompletedCallback).ConfigureAwait(false);
    }

    private async void OnGetCredentialCompletedCallback(object? sender, WebAuthnCompletedEventArgs<AuthenticatorAssertionRawResponse> args)
    {
        if (args.Successful)
        {
            string key = RandomNumberGenerator.GetHexString(32);
            Model.AuthenticatorDataAccessKey = key;
            StateHasChanged();

            string cacheKey = await GetSecurityKeyDataCacheKeyAsync(key).ConfigureAwait(false);
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
            SnackbarService.AddError(args.ErrorMessage ?? Localizer["webAuthn_Error"]);
        }
    }

    private async Task Verify2faAsync()
    {
        SignInResult verifyResult = await (Model.Method switch
        {
            TwoFactorMethod.Authenticator => SignInManager.TwoFactorAuthenticatorSignInAsync(Model.TrimmedCode, Persistent, Model.RememberClient),
            TwoFactorMethod.Email => SignInManager.TwoFactorEmailSignInAsync(Model.TrimmedCode, Persistent, Model.RememberClient),
            TwoFactorMethod.SecurityKey => VerifyTwoFactorSecurityKeyAsync(Model.AuthenticatorDataAccessKey, Persistent, Model.RememberClient),
            TwoFactorMethod.Recovery => SignInManager.TwoFactorRecoveryCodeSignInAsync(Model.TrimmedCode),
            _ => Task.FromResult(SignInResult.Failed)
        }).ConfigureAwait(false);

        switch (verifyResult)
        {
            case { Succeeded: true }:
                NavigationManager.NavigateSaveTo(ReturnUrl ?? Routes.PagesIndex());
                break;
            case { IsLockedOut: true }:
                DateTimeOffset lockOutEnd = (await UserManager.GetLockoutEndDateAsync(_user).ConfigureAwait(false)).Value;

                _errorMessage = lockOutEnd.Offset <= TimeSpan.MaxValue     // MaxValue means that the user is locked without an end. It has to unlocked manually.
                    ? Localizer["signIn_LockedOut", lockOutEnd.Humanize()]
                    : Localizer["signIn_LockedOut_NotSpecified"];
                break;
            default:
                _errorMessage = Localizer["signIn_" + verifyResult];
                break;
        }
    }

    private async Task<SignInResult> VerifyTwoFactorSecurityKeyAsync(string? dataAccessKey, bool isPersistent, bool rememberClient)
    {
        if (string.IsNullOrWhiteSpace(dataAccessKey))
        {
            return SignInResult.Failed;
        }

        string cacheKey = await GetSecurityKeyDataCacheKeyAsync(dataAccessKey).ConfigureAwait(false);
        if (!Cache.TryGetValue(cacheKey, out SecurityKeyAuthState? authState))
        {
            return SignInResult.Failed;
        }

        Cache.Remove(dataAccessKey!);
        return await SignInManager.TwoFactorFido2CredentialSignInAsync(authState!.Options, authState.Response, isPersistent, rememberClient).ConfigureAwait(false);
    }

    private async Task<string> GetSecurityKeyDataCacheKeyAsync(string key)
    {
        string userId = await UserManager.GetUserIdAsync(_user).ConfigureAwait(false);
        return $"twoFactor_securityKeyData_{userId}_{key}";
    }

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
