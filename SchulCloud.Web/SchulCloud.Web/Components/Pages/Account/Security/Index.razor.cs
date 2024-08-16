using BlazorBootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Database.Models;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Identity.Managers;

namespace SchulCloud.Web.Components.Pages.Account.Security;

[Authorize]
[Route("/account/security")]
public sealed partial class Index : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<Index> Localizer { get; set; } = default!;

    [Inject]
    private IOptions<PasswordOptions> PasswordOptionsAccessor { get; set; } = default!;

    [Inject]
    private SchulCloudUserManager UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityErrorDescriber ErrorDescriber { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private ModalService ModalService { get; set; } = default!;
    #endregion

    private User _user = default!;
    private bool _mfaEnabled;
    private int _mfaRemainingRecoveryCodes;
    private bool _mfaEmailEnabled;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState.ConfigureAwait(false);
        _user = (await UserManager.GetUserAsync(authenticationState.User).ConfigureAwait(false))!;

        _mfaEnabled = await UserManager.GetTwoFactorEnabledAsync(_user).ConfigureAwait(false);
        _mfaRemainingRecoveryCodes = await UserManager.CountRecoveryCodesAsync(_user).ConfigureAwait(false);
        _mfaEmailEnabled = await UserManager.GetTwoFactorEmailEnabledAsync(_user).ConfigureAwait(false);
    }

    private async Task AuthenticatorDisable_ClickAsync()
    {
        await ModalService.ShowAsync(new()
        {
            Title = Localizer["authenticator_Disable"],
            Message = Localizer["authenticator_DisableMessage"],
            ShowFooterButton = true,
            FooterButtonColor = ButtonColor.Danger,
            FooterButtonText = Localizer["authenticator_DisableExecuteBtn"]
        }, AuthenticatorDisable_CallbackAsync);
    }

    private async void AuthenticatorDisable_CallbackAsync()
    {
        IdentityResult result = await UserManager.SetTwoFactorEnabledAsync(_user, false).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (result.Succeeded)
            {
                ToastService.NotifySuccess(Localizer["authenticator_Disable_Success"], Localizer["authenticator_Disable_SuccessMessage"]);
                _mfaEnabled = false;
            }
            else
            {
                ToastService.NotifyError(result.Errors, Localizer["disable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async Task EmailEnable_ClickAsync()
    {
        IdentityResult result = await UserManager.SetTwoFactorEmailEnabledAsync(_user, true).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (result.Succeeded)
            {
                ToastService.NotifySuccess(Localizer["email_Enable_Success"], Localizer["email_Enable_SuccessMessage"]);
                _mfaEmailEnabled = true;
            }
            else
            {
                ToastService.NotifyError(result.Errors, Localizer["enable_Error"]);
            }
        }).ConfigureAwait(false);
    }

    private async void EmailDisable_ClickAsync()
    {
        IdentityResult result = await UserManager.SetTwoFactorEmailEnabledAsync(_user, false).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (result.Succeeded)
            {
                ToastService.NotifySuccess(Localizer["email_Disable_Success"], Localizer["email_Disable_SuccessMessage"]);
                _mfaEmailEnabled = false;
            }
            else
            {
                ToastService.NotifyError(result.Errors, Localizer["disable_Error"]);
            }

            StateHasChanged();
        }).ConfigureAwait(false);
    }
}
