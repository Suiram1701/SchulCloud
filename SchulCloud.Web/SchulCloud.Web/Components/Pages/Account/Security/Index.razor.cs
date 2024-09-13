using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MudBlazor;
using SchulCloud.Store.Managers;
using SchulCloud.Web.Components.Dialogs;
using SchulCloud.Web.Extensions;

namespace SchulCloud.Web.Components.Pages.Account.Security;

[Route("/account/security")]
public sealed partial class Index : ComponentBase, IDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<Index> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private IOptions<PasswordOptions> PasswordOptionsAccessor { get; set; } = default!;

    [Inject]
    private SchulCloudUserManager<ApplicationUser, AppCredential> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityErrorDescriber ErrorDescriber { get; set; } = default!;

    [Inject]
    private PersistentComponentState ComponentState { get; set; } = default!;
    #endregion

    private PersistingComponentStateSubscription? _persistingSubscription;

    private ApplicationUser _user = default!;
    private int _passkeysCount;
    private bool _passkeysEnabled;
    private bool _mfaEnabled;
    private bool _mfaEmailEnabled;
    private int _securityKeysCount;
    private bool _mfaSecurityKeyEnabled;
    private int _mfaRemainingRecoveryCodes;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState.ConfigureAwait(false);
        if (ComponentState.TryTakeFromJson("state", out SecurityState? state))
        {
            state!.Deconstruct(
                out _passkeysCount,
                out _passkeysEnabled,
                out _mfaEnabled,
                out _mfaEmailEnabled,
                out _securityKeysCount,
                out _mfaSecurityKeyEnabled,
                out _mfaRemainingRecoveryCodes);

            _user = (await UserManager.GetUserAsync(authenticationState.User).ConfigureAwait(false))!;
        }
        else
        {
            _user = (await UserManager.GetUserAsync(authenticationState.User).ConfigureAwait(false))!;

            _passkeysEnabled = await UserManager.GetPasskeySignInEnabledAsync(_user).ConfigureAwait(false);
            _passkeysCount = await UserManager.GetPasskeyCountAsync(_user).ConfigureAwait(false);
            await UpdateMfaStatesAsync().ConfigureAwait(false);

            _persistingSubscription = ComponentState.RegisterOnPersisting(() =>
            {
                SecurityState state = new(
                    _passkeysCount,
                    _passkeysEnabled,
                    _mfaEnabled,
                    _mfaEmailEnabled,
                    _securityKeysCount,
                    _mfaSecurityKeyEnabled,
                    _mfaRemainingRecoveryCodes);
                ComponentState.PersistAsJson("state", state);

                return Task.CompletedTask;
            });
        }
    }

    private async Task SetPasskeysEnabled_ClickAsync(bool enabled)
    {
        if (_passkeysCount <= 0 && enabled)
        {
            return;
        }

        IdentityResult result = await UserManager.SetPasskeySignInEnabledAsync(_user, enabled).ConfigureAwait(false);
        if (result.Succeeded)
        {
            _passkeysEnabled = enabled;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        else
        {
            string messageKey = enabled
                ? "enable_Error"
                : "disable_Error";
            SnackbarService.AddError(result.Errors, Localizer[messageKey]);
        }
    }

    private async Task AuthenticatorDisable_ClickAsync()
    {
        IDialogReference dialogReference = await DialogService.ShowConfirmDialogAsync(
            Localizer["authenticator_DisableBtn"],
            Localizer["authenticator_DisableMessage"],
            confirmColor: Color.Error);
        if (await dialogReference.GetReturnValueAsync<bool?>() ?? false)
        {
            IdentityResult disableResult = await UserManager.SetTwoFactorEnabledAsync(_user, false).ConfigureAwait(false);
            if (disableResult.Succeeded)
            {
                await UpdateMfaStatesAsync().ConfigureAwait(false);
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
            else
            {
                SnackbarService.AddError(disableResult.Errors, Localizer["disable_Error"]);
            }
        }
    }

    private async Task SetEmailEnabled_ClickAsync(bool enabled)
    {
        if (!_mfaEnabled && enabled)
        {
            return;
        }

        IdentityResult result = await UserManager.SetTwoFactorEmailEnabledAsync(_user, enabled).ConfigureAwait(false);
        if (result.Succeeded)
        {
            _mfaEmailEnabled = enabled;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        else
        {
            string messageKey = enabled
                ? "enable_Error"
                : "disable_Error";
            SnackbarService.AddError(result.Errors, Localizer[messageKey]);
        }
    }

    private async Task SetSecurityKeyEnabled_ClickAsync(bool enabled)
    {
        if ((!_mfaEnabled || _securityKeysCount <= 0) && enabled)
        {
            return;
        }

        IdentityResult result = await UserManager.SetTwoFactorSecurityKeyEnabledAsync(_user, enabled).ConfigureAwait(false);
        if (result.Succeeded)
        {
            _mfaSecurityKeyEnabled = enabled;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        else
        {
            string messageKey = enabled
                ? "enable_Error"
                : "disable_Error";
            SnackbarService.AddError(result.Errors, Localizer[messageKey]);
        }
    }

    private async Task GenerateNewRecoveryCodes_ClickAsync()
    {
        IDialogReference dialogReference = await DialogService.ShowConfirmDialogAsync(Localizer["recovery_RenewBtn"], Localizer["recovery_RenewMessage"]);
        if (await dialogReference.GetReturnValueAsync<bool?>() ?? false)
        {
            NavigationManager.NavigateToRecoveryCodes();
        }
    }

    private async Task UpdateMfaStatesAsync()
    {
        _mfaEnabled = await UserManager.GetTwoFactorEnabledAsync(_user).ConfigureAwait(false);
        if (_mfaEnabled)
        {
            _mfaEmailEnabled = await UserManager.GetTwoFactorEmailEnabledAsync(_user).ConfigureAwait(false);
            _securityKeysCount = await UserManager.GetTwoFactorSecurityKeysCountAsync(_user).ConfigureAwait(false);
            _mfaSecurityKeyEnabled = await UserManager.GetTwoFactorSecurityKeyEnableAsync(_user).ConfigureAwait(false);
            _mfaRemainingRecoveryCodes = await UserManager.CountRecoveryCodesAsync(_user).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _persistingSubscription?.Dispose();
    }

    private record SecurityState(
        int PasskeysCount,
        bool PasskeysEnabled,
        bool MfaEnabled,
        bool MfaEmailEnabled,
        int SecurityKeysCount,
        bool MfaSecurityKeysEnabled,
        int MfaRemainingRecoveryCodes);
}