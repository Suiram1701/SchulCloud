using BlazorBootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Store.Managers;
using SchulCloud.Web.Components.Dialogs;
using SchulCloud.Web.Extensions;

namespace SchulCloud.Web.Components.Pages.Account.Security;

[Route("/account/security")]
public sealed partial class Index : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<Index> Localizer { get; set; } = default!;

    [Inject]
    private IOptions<PasswordOptions> PasswordOptionsAccessor { get; set; } = default!;

    [Inject]
    private SchulCloudUserManager<ApplicationUser, AppCredential> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityErrorDescriber ErrorDescriber { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;
    #endregion

    private RemoveDialog _removeDialog = default!;

    private ApplicationUser _user = default!;
    private bool _passkeysEnabled;
    private bool _mfaEnabled;
    private bool _mfaEmailEnabled;
    private bool _mfaSecurityKeyEnabled;
    private int _mfaRemainingRecoveryCodes;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState.ConfigureAwait(false);
        _user = (await UserManager.GetUserAsync(authenticationState.User).ConfigureAwait(false))!;

        _passkeysEnabled = await UserManager.GetPasskeySignInEnabledAsync(_user).ConfigureAwait(false);
        await UpdateMfaStates().ConfigureAwait(false);
    }

    private async Task PasskeysEnable_ClickAsync()
    {
        IdentityResult enableResult = await UserManager.SetPasskeySignInEnabledAsync(_user, true).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (enableResult.Succeeded)
            {
                _passkeysEnabled = true;
                StateHasChanged();
            }
            else
            {
                ToastService.NotifyError(enableResult.Errors, Localizer["enable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async Task PasskeysDisable_ClickAsync()
    {
        IdentityResult disableResult = await UserManager.SetPasskeySignInEnabledAsync(_user, false).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (disableResult.Succeeded)
            {
                _passkeysEnabled = false;
                StateHasChanged();
            }
            else
            {
                ToastService.NotifyError(disableResult.Errors, Localizer["disable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async Task AuthenticatorDisable_ClickAsync()
    {
        if (!await _removeDialog.ShowAsync(Localizer["authenticator_Disable"], Localizer["authenticator_DisableMessage"]).ConfigureAwait(false))
        {
            return;
        }

        IdentityResult result = await UserManager.SetTwoFactorEnabledAsync(_user, false).ConfigureAwait(false);
        await InvokeAsync(async () =>
        {
            if (result.Succeeded)
            {
                await UpdateMfaStates().ConfigureAwait(false);
                StateHasChanged();
            }
            else
            {
                ToastService.NotifyError(result.Errors, Localizer["disable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async Task EmailEnable_ClickAsync()
    {
        if (!_mfaEnabled)
        {
            return;
        }

        IdentityResult enableResult = await UserManager.SetTwoFactorEmailEnabledAsync(_user, true).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (enableResult.Succeeded)
            {
                _mfaEmailEnabled = true;
                StateHasChanged();
            }
            else
            {
                ToastService.NotifyError(enableResult.Errors, Localizer["enable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async Task EmailDisable_ClickAsync()
    {
        IdentityResult disableResult = await UserManager.SetTwoFactorEmailEnabledAsync(_user, false).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (disableResult.Succeeded)
            {
                _mfaEmailEnabled = false;
                StateHasChanged();
            }
            else
            {
                ToastService.NotifyError(disableResult.Errors, Localizer["disable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async Task SecurityKeyEnable_ClickAsync()
    {
        if (!_mfaEnabled)
        {
            return;
        }

        IdentityResult enableResult = await UserManager.SetTwoFactorSecurityKeyEnabledAsync(_user, true).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (enableResult.Succeeded)
            {
                _mfaSecurityKeyEnabled = true;
                StateHasChanged();
            }
            else
            {
                ToastService.NotifyError(enableResult.Errors, Localizer["enable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async Task SecurityKeyDisable_ClickAsync()
    {
        IdentityResult disableResult = await UserManager.SetTwoFactorSecurityKeyEnabledAsync(_user, false).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (disableResult.Succeeded)
            {
                _mfaSecurityKeyEnabled = false;
                StateHasChanged();
            }
            else
            {
                ToastService.NotifyError(disableResult.Errors, Localizer["disable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async Task UpdateMfaStates()
    {
        _mfaEnabled = await UserManager.GetTwoFactorEnabledAsync(_user).ConfigureAwait(false);
        if (_mfaEnabled)
        {
            _mfaEmailEnabled = await UserManager.GetTwoFactorEmailEnabledAsync(_user).ConfigureAwait(false);
            _mfaSecurityKeyEnabled = await UserManager.GetTwoFactorSecurityKeyEnableAsync(_user).ConfigureAwait(false);
            _mfaRemainingRecoveryCodes = await UserManager.CountRecoveryCodesAsync(_user).ConfigureAwait(false);
        }
    }

    private bool? IsMfaMethodEnabled(bool methodState) => _mfaEnabled ? methodState : null;
}
