using BlazorBootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Database.Models;
using SchulCloud.Web.Extensions;

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
    private UserManager<User> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityErrorDescriber ErrorDescriber { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;
    #endregion

    private User _user = default!;
    private bool _mfaEnabled;

    private Modal? AuthenticatorDeactivateModal { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState.ConfigureAwait(false);
        _user = (await UserManager.GetUserAsync(authenticationState.User).ConfigureAwait(false))!;

        _mfaEnabled = await UserManager.GetTwoFactorEnabledAsync(_user).ConfigureAwait(false);
    }

    private async Task AuthenticatorDeactivate_ClickAsync()
    {
        await AuthenticatorDeactivateModal!.ShowAsync().ConfigureAwait(false);
    }

    private async Task AuthenticatorModalAbort_ClickAsync()
    {
        await AuthenticatorDeactivateModal!.HideAsync().ConfigureAwait(false);
    }

    private async Task AuthenticatorModalExecute_ClickAsync()
    {
        IdentityResult result = await UserManager.SetTwoFactorEnabledAsync(_user, false).ConfigureAwait(false);
        await InvokeAsync(() =>
        {
            if (result.Succeeded)
            {
                ToastService.NotifySuccess(Localizer["authenticator_DeactivateSuccess_Title"], Localizer["authenticator_DeactivateSuccess_Message"]);
                _mfaEnabled = false;
            }
            else
            {
                ToastService.NotifyError(result.Errors, Localizer["authenticator_Deactivate_ErrorTitle"]);
            }
        }).ConfigureAwait(false);
    }
}
