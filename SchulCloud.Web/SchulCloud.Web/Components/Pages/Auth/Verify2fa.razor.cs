using BlazorBootstrap;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SchulCloud.Database.Models;
using SchulCloud.Web.Constants;
using SchulCloud.Web.Enums;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Identity.Managers;
using SchulCloud.Web.Models;
using SchulCloud.Web.Services.Interfaces;

namespace SchulCloud.Web.Components.Pages.Auth;

[Route("/auth/verify2fa")]
public sealed partial class Verify2fa : ComponentBase, IDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<Verify2fa> Localizer { get; set; } = default!;

    [Inject]
    private Identity.EmailSenders.IEmailSender<User> EmailSender { get; set; } = default!;

    [Inject]
    private IRequestLimiter<User> Limiter { get; set; } = default!;

    [Inject]
    private AntiforgeryStateProvider AntiforgeryStateProvider { get; set; } = default!;

    [Inject]
    private SchulCloudSignInManager SignInManager { get; set; } = default!;

    [Inject]
    private SchulCloudUserManager UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private PersistentComponentState ComponentState { get; set; } = default!;
    #endregion

    private const string _formName = "verify2fa";

    private User _user = default!;
    private bool _mfaEmailEnabled;
    private string? _errorMessage;
    private PersistingComponentStateSubscription? _persistingSubscription;

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

        if (ComponentState.TryTakeFromJson(nameof(_user), out User? user))
        {
            _user = user!;
        }
        ComponentState.TryTakeFromJson(nameof(_mfaEmailEnabled), out _mfaEmailEnabled);

        if (HttpContext is not null)
        {
            user ??= await SignInManager.GetTwoFactorAuthenticationUserAsync().ConfigureAwait(false);
            if (user is not null)
            {
                _user = user!;
                _mfaEmailEnabled = await UserManager.GetTwoFactorEmailEnabledAsync(_user).ConfigureAwait(false);

                ComponentState.RegisterOnPersisting(() =>
                {
                    ComponentState.PersistAsJson(nameof(_user), _user);
                    ComponentState.PersistAsJson(nameof(_mfaEmailEnabled), _mfaEmailEnabled);

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

    private async Task Verify2faAsync()
    {
        SignInResult result = await (Model.Method switch
        {
            TwoFactorMethod.Authenticator => SignInManager.TwoFactorAuthenticatorSignInAsync(Model.TrimmedCode, Persistent, Model.RememberClient),
            TwoFactorMethod.Email => SignInManager.TwoFactorEmailSignInAsync(Model.TrimmedCode, Persistent, Model.RememberClient),
            TwoFactorMethod.Recovery => SignInManager.TwoFactorRecoveryCodeSignInAsync(Model.TrimmedCode),
            _ => Task.FromResult(SignInResult.Failed)
        }).ConfigureAwait(false);

        switch (result)
        {
            case { Succeeded: true }:
                Uri returnUri = NavigationManager.ToAbsoluteUri(ReturnUrl);
                NavigationManager.NavigateTo(returnUri.PathAndQuery);     // prevent a redirect to another domain by using only the path and query part.
                break;
            case { IsLockedOut: true }:
                DateTimeOffset lockOutEnd = (await UserManager.GetLockoutEndDateAsync(_user).ConfigureAwait(false)).Value;

                _errorMessage = lockOutEnd.UtcDateTime <= DateTime.MaxValue     // MaxValue means that the user is locked without an end. It has to unlocked manually.
                    ? Localizer["signIn_LockedOut", lockOutEnd.Humanize()]
                    : Localizer["signIn_LockedOut_NotSpecified"];
                break;
            default:
                _errorMessage = Localizer["signIn_" + result];
                break;
        }
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
            ToastService.Notify(new(ToastType.Info, Localizer["emailConfirmation_Timeout"], Localizer["emailConfirmation_TimeoutMessage", expiration.Humanize()])
            {
                AutoHide = true,
            });
        }
        else
        {
            // Renew the user instance because a persistent instance isn't changed tracked anymore.
            string userId = await UserManager.GetUserIdAsync(_user).ConfigureAwait(false);
            User user = (await UserManager.FindByIdAsync(userId).ConfigureAwait(false))!;

            string code = await UserManager.GenerateTwoFactorEmailCodeAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(code))
            {
                throw new Exception("An unknown error occurred during the email code generation.");
            }

            // Show the Toast before the email is sent for better user experience (sending the mail is time expensive).
            await InvokeAsync(async () =>
            {
                string anonymizedAddress = await UserManager.GetAnonymizedEmailAsync(user).ConfigureAwait(false);
                ToastService.Notify(new(ToastType.Info, Localizer["emailConfirmation"], Localizer["emailConfirmationMessage", anonymizedAddress])
                {
                    AutoHide = true
                });
            }).ConfigureAwait(false);

            string email = (await UserManager.GetEmailAsync(user).ConfigureAwait(false))!;
            await EmailSender.Send2faEmailCodeAsync(user, email, code).ConfigureAwait(false);
        }
    }

    private void Input_Changed()
    {
        _errorMessage = null;
    }

    private bool IsInvalid() => _errorMessage is not null;

    private string IsInvalidInputClass => IsInvalid() ? ExtendedBootstrapClass.IsInvalid : string.Empty;

    public void Dispose()
    {
        _persistingSubscription?.Dispose();
    }
}
