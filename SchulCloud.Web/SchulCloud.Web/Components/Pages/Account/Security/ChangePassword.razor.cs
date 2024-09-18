using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Models;

namespace SchulCloud.Web.Components.Pages.Account.Security;

[Route("/account/security/changePassword")]
public sealed partial class ChangePassword : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ChangePassword> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IPasswordValidator<ApplicationUser> PasswordValidator { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityErrorDescriber ErrorDescriber { get; set; } = default!;
    #endregion

    private ApplicationUser _user = default!;
    private readonly PasswordChangeModel _model = new();

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (!UserManager.SupportsUserPassword)
        {
            NavigationManager.NavigateToSecurityIndex();
            return;
        }

        AuthenticationState state = await AuthenticationState;
        _user = (await UserManager.GetUserAsync(state.User))!;
    }

    private async Task Form_OnValidSubmitAsync()
    {
        IdentityResult result = await UserManager.ChangePasswordAsync(_user, _model.CurrentPassword, _model.NewPassword);
        if (result.Succeeded)
        {
            SnackbarService.AddSuccess(Localizer["changeSuccess"]);
            NavigationManager.NavigateToSecurityIndex();
        }
        else
        {
            SnackbarService.AddError(result.Errors, Localizer["changeError"]);
        }
    }

    private async Task<IEnumerable<string>?> Form_CurrentPasswordValidateAsync()
    {
        if (!await UserManager.CheckPasswordAsync(_user, _model.CurrentPassword))
        {
            return [ErrorDescriber.PasswordMismatch().Description];
        }

        return null;
    }

    private async Task<IEnumerable<string>?> Form_NewPasswordValidateAsync()
    {
        if (string.IsNullOrWhiteSpace(_model.NewPassword))
        {
            return [Localizer["form_NotEmpty"]];
        }

        IdentityResult validateResult = await PasswordValidator.ValidateAsync(UserManager, _user!, _model.NewPassword);
        return validateResult.Errors.Select(error => error.Description);
    }

    private IEnumerable<string>? Form_ConfirmedPasswordValidate()
    {
        if (string.IsNullOrWhiteSpace(_model.ConfirmedPassword))
        {
            return [Localizer["form_NotEmpty"]];
        }

        return !_model.NewPassword.Equals(_model.ConfirmedPassword)
            ? [Localizer["form_ConfirmedPassword_NotMatch"]]
            : null;
    }
}
