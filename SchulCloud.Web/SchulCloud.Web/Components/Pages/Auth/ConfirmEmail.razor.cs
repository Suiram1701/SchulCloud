using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Services.Interfaces;

namespace SchulCloud.Web.Components.Pages.Auth;

[Route("/auth/confirmEmail")]
public sealed partial class ConfirmEmail : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ConfirmEmail> Localizer { get; set; } = default!;

    [Inject]
    private Identity.EmailSenders.IEmailSender<ApplicationUser> EmailSender { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IRequestLimiter<ApplicationUser> RequestLimiter { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private ApplicationUser _user = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public string UserId { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "token")]
    public string? ConfirmToken { get; set; }

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        ApplicationUser? user = !string.IsNullOrWhiteSpace(UserId)
            ? await UserManager.FindByIdAsync(UserId)
            : null;
        if (user is null)
        {
            NavigationManager.NavigateToLogin();
            return;
        }
        _user = user;

        if (await UserManager.IsEmailConfirmedAsync(_user))
        {
            NavigationManager.NavigateSaveTo(ReturnUrl ?? Routes.Dashboard());
            return;
        }

        if (!string.IsNullOrWhiteSpace(ConfirmToken))
        {
            IdentityResult confirmResult = await UserManager.ConfirmEmailAsync(_user, ConfirmToken);
            if (confirmResult.Succeeded)
            {
                NavigationManager.NavigateSaveTo(ReturnUrl ?? Routes.Dashboard());
            }
            else
            {
                SnackbarService.AddError(confirmResult.Errors, Localizer["confirmError"]);
            }
        }
    }

    private async Task SendConfirmEmail_ClickAsync()
    {
        if (await RequestLimiter.CanRequestEmailConfirmationAsync(_user))
        {
            // Show the snackbar before the email is sent for better user experience (sending an mail is time expensive).
            string anonymizedAddress = await UserManager.GetAnonymizedEmailAsync(_user);
            SnackbarService.AddInfo(Localizer["sentEmail", anonymizedAddress]);

            string confirmToken = await UserManager.GenerateEmailConfirmationTokenAsync(_user);
            Uri confirmUri = NavigationManager.ToAbsoluteUri(Routes.ConfirmEmail(userId: UserId, token: confirmToken, returnUrl: ReturnUrl));

            string userEmail = (await UserManager.GetEmailAsync(_user))!;
            await EmailSender.SendEmailConfirmLinkAsync(_user, userEmail, confirmUri.AbsoluteUri);
        }
        else
        {
            DateTimeOffset? timeout = await RequestLimiter.GetEmailConfirmationTimeoutAsync(_user);
            SnackbarService.AddInfo(Localizer["sentEmail_Timeout", timeout.Humanize()]);
        }
    }
}
