using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SchulCloud.Frontend.Extensions;
using SchulCloud.Frontend.Models;
using SchulCloud.Frontend.Services.Interfaces;

namespace SchulCloud.Frontend.Components.Pages.Auth;

[Route("/auth/resetPassword")]
public sealed partial class ResetPassword : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ResetPassword> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IPasswordValidator<ApplicationUser> PasswordValidator { get; set; } = default!;

    [Inject]
    private Identity.EmailSenders.IEmailSender<ApplicationUser> EmailSender { get; set; } = default!;

    [Inject]
    private IRequestLimiter<ApplicationUser> ResetLimiter { get; set; } = default!;

    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private MudForm _userForm = default!;

    private ApplicationUser? _user;
    private readonly PasswordResetModel _model = new();

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public string? UserId { get; set; }

    [SupplyParameterFromQuery(Name = "token")]
    public string? ChangeToken { get; set; }

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState;
        if (SignInManager.IsSignedIn(state.User))
        {
            UserId ??= UserManager.GetUserId(state.User);
        }

        if (UserId is not null)
        {
            _user = await UserManager.FindByIdAsync(UserId);
        }
    }

    private async Task SendResetCode_ClickAsync()
    {
        if (_user is null)
        {
            return;
        }

        if (await ResetLimiter.CanRequestPasswordResetAsync(_user))
        {
            // Show the snackbar before the email is sent for better user experience (sending the mail is time expensive).
            string anonymizedAddress = await UserManager.GetAnonymizedEmailAsync(_user);
            SnackbarService.AddInfo(Localizer["sent", anonymizedAddress]);

            string resetToken = await UserManager.GeneratePasswordResetTokenAsync(_user);
            Uri resetUri = NavigationManager.ToAbsoluteUri(Routes.ResetPassword(userId: UserId, token: resetToken, returnUrl: ReturnUrl));

            string userEmail = (await UserManager.GetEmailAsync(_user))!;
            await EmailSender.SendPasswordResetLinkAsync(_user, userEmail, resetUri.AbsoluteUri);
        }
        else
        {
            DateTimeOffset? expiration = await ResetLimiter.GetPasswordResetTimeoutAsync(_user);
            SnackbarService.AddInfo(Localizer["sent_Timeout", expiration.Humanize()]);
        }
    }

    private async Task UserForm_ValidChangedAsync(bool valid)
    {
        if (valid)
        {
            UserId = await UserManager.GetUserIdAsync(_user!);
            NavigationManager.NavigateToResetPassword(userId: UserId, returnUrl: ReturnUrl, replace: true);
        }
    }

    private async Task<string?> UserForm_UserValidateAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Localizer["form_NotEmpty"];
        }

        _user = await UserManager.FindByEmailAsync(value);
        _user ??= await UserManager.FindByNameAsync(value);
        if (_user is null)
        {
            return Localizer["userForm_User_NotFound"];
        }

        return null;
    }

    private async Task ResetForm_OnValidSubmitAsync()
    {
        if (_user is null || ChangeToken is null)
        {
            return;
        }

        string decodedToken = Uri.UnescapeDataString(ChangeToken);

        IdentityResult result = await UserManager.ResetPasswordAsync(_user, decodedToken, _model.NewPassword);
        if (result.Succeeded)
        {
            SnackbarService.AddSuccess(Localizer["success"]);
            NavigationManager.NavigateSaveTo(ReturnUrl ?? Routes.Login());
        }
        else
        {
            SnackbarService.AddError(result.Errors, Localizer["error"]);
        }
    }

    private async Task<IEnumerable<string>?> ResetForm_NewPasswordValidateAsync()
    {
        if (string.IsNullOrWhiteSpace(_model.NewPassword))
        {
            return [Localizer["form_NotEmpty"]];
        }

        IdentityResult validateResult = await PasswordValidator.ValidateAsync(UserManager, _user!, _model.NewPassword);
        return validateResult.Errors.Select(error => error.Description);
    }

    private IEnumerable<string>? ResetForm_ConfirmedPasswordValidate()
    {
        if (string.IsNullOrWhiteSpace(_model.ConfirmedPassword))
        {
            return [Localizer["form_NotEmpty"]];
        }

        return !_model.NewPassword.Equals(_model.ConfirmedPassword)
            ? [Localizer["resetForm_ConfirmedPassword_NotMatch"]]
            : null;
    }
}
