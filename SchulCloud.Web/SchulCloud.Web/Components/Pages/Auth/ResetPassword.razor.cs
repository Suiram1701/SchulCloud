using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Models;
using SchulCloud.Web.Services.Interfaces;

namespace SchulCloud.Web.Components.Pages.Auth;

[Route("/auth/resetPassword")]
public sealed partial class ResetPassword : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ResetPassword> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

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
        AuthenticationState state = await AuthenticationState.ConfigureAwait(false);
        if (SignInManager.IsSignedIn(state.User))
        {
            UserId ??= UserManager.GetUserId(state.User);
        }

        if (UserId is not null)
        {
            _user = await UserManager.FindByIdAsync(UserId).ConfigureAwait(false);
        }
    }

    private async Task SendResetCode_ClickAsync()
    {
        if (_user is null)
        {
            return;
        }

        if (await ResetLimiter.CanRequestPasswordResetAsync(_user).ConfigureAwait(false))
        {
            // Show the snackbar before the email is sent for better user experience (sending the mail is time expensive).
            string anonymizedAddress = await UserManager.GetAnonymizedEmailAsync(_user).ConfigureAwait(false);
            SnackbarService.AddInfo(Localizer["sent", anonymizedAddress]);

            string resetToken = await UserManager.GeneratePasswordResetTokenAsync(_user).ConfigureAwait(false);
            Uri resetUri = NavigationManager.ToAbsoluteUri(Routes.ResetPassword(userId: UserId, token: resetToken, returnUrl: ReturnUrl));

            string userEmail = (await UserManager.GetEmailAsync(_user).ConfigureAwait(false))!;
            await EmailSender.SendPasswordResetLinkAsync(_user, userEmail, resetUri.AbsoluteUri).ConfigureAwait(false);
        }
        else
        {
            DateTimeOffset? expiration = await ResetLimiter.GetPasswordResetExpirationTimeAsync(_user).ConfigureAwait(false);
            SnackbarService.AddInfo(Localizer["sent_Timeout", expiration.Humanize()]);
        }
    }

    private async Task<IEnumerable<string>> ValidateUserAsync()
    {
        _user = await UserManager.FindByEmailAsync(_model.User).ConfigureAwait(false);
        _user ??= await UserManager.FindByNameAsync(_model.User).ConfigureAwait(false);

        if (_user is null)
        {
            return [Localizer["userForm_UserNotFound"]];
        }

        UserId = await UserManager.GetUserIdAsync(_user).ConfigureAwait(false);
        NavigationManager.NavigateToResetPassword(userId: UserId, returnUrl: ReturnUrl, replace: true);

        return [];
    }

    private async Task ResetPasswordAsync()
    {
        if (_user is null || ChangeToken is null)
        {
            return;
        }

        string decodedToken = Uri.UnescapeDataString(ChangeToken);

        IdentityResult result = await UserManager.ResetPasswordAsync(_user, decodedToken, _model.NewPassword).ConfigureAwait(false);
        if (result.Succeeded)
        {
            SnackbarService.AddSuccess(Localizer["success"]);
            NavigationManager.NavigateSaveTo(ReturnUrl ?? Routes.SignIn());
        }
        else
        {
            SnackbarService.AddError(result.Errors, Localizer["error"]);
        }
    }
}
