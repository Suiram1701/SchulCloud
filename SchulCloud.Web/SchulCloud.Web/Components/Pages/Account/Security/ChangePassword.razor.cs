using BlazorBootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SchulCloud.Database.Models;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Models;

namespace SchulCloud.Web.Components.Pages.Account.Security;

[Authorize]
[Route("/account/security/changePassword")]
public sealed partial class ChangePassword : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ChangePassword> Localizer { get; set; } = default!;

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
    private readonly PasswordChangeModel _model = new();

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState.ConfigureAwait(false);
        _user = (await UserManager.GetUserAsync(state.User).ConfigureAwait(false))!;
    }

    private async Task<IEnumerable<string>> ValidateCurrentPasswordAsync(EditContext context, FieldIdentifier identifier)
    {
        if (!await UserManager.CheckPasswordAsync(_user, _model.CurrentPassword).ConfigureAwait(false))
        {
            return [ErrorDescriber.PasswordMismatch().Description];
        }

        return [];
    }

    private async Task OnValidSubmitAsync()
    {
        IdentityResult result = await UserManager.ChangePasswordAsync(_user, _model.CurrentPassword, _model.NewPassword).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await InvokeAsync(() => ToastService.NotifySuccess(Localizer["successToast_Title"], Localizer["successToast_Message"])).ConfigureAwait(false);
            NavigationManager.NavigateToSecurityIndex();
        }
        else
        {
            await InvokeAsync(() => ToastService.NotifyError(result.Errors, Localizer["errorToast_Title"])).ConfigureAwait(false);
        }
    }
}
