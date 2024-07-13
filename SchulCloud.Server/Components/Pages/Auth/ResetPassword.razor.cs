using BlazorBootstrap;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using SchulCloud.Database.Models;
using SchulCloud.Server.Extensions;
using SchulCloud.Server.Models;
using SchulCloud.Server.Services.Interfaces;

namespace SchulCloud.Server.Components.Pages.Auth;

[AllowAnonymous]
[Route("/auth/resetPassword")]
public sealed partial class ResetPassword : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ResetPassword> Localizer { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private IEmailSender<User> EmailSender { get; set; } = default!;

    [Inject]
    private IPasswordResetLimiter<User> ResetLimiter { get; set; } = default!;

    [Inject]
    private SignInManager<User> SignInManager { get; set; } = default!;

    [Inject]
    private UserManager<User> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;
    #endregion

    private User? _user;

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

        if (!ResetLimiter.CanRequestPasswordReset(_user))
        {
            DateTimeOffset expiration = ResetLimiter.GetExpirationTime(_user);
            ToastService.Notify(new(ToastType.Info, Localizer["sentToastTitle_Timeout"], Localizer["sentToastMessage_Timeout", expiration.Humanize()])
            {
                AutoHide = true,
            });

            return;
        }

        string resetToken = await UserManager.GeneratePasswordResetTokenAsync(_user).ConfigureAwait(false);
        string resetUrl = NavigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["userId"] = UserId,
            ["token"] = Uri.EscapeDataString(resetToken),
            ["returnUrl"] = ReturnUrl
        });

        await EmailSender.SendPasswordResetLinkAsync(_user, _user.Email!, resetUrl).ConfigureAwait(false);

        string blurredAddress = _user.GetAnonymizedEmail();
        ToastService.Notify(new(ToastType.Info, Localizer["sentToastTitle"], Localizer["sentToastMessage", blurredAddress])
        {
            AutoHide = true
        });
    }

    private async Task<IEnumerable<string>> ValidateUserAsync(EditContext context, FieldIdentifier identifier)
    {
        _user = await UserManager.FindByEmailAsync(_model.User).ConfigureAwait(false);
        _user ??= await UserManager.FindByNameAsync(_model.User).ConfigureAwait(false);

        if (_user is null)
        {
            return [ Localizer["userForm_NotFound"] ];
        }

        UserId = _user.Id;

        string newUri = NavigationManager.GetUriWithQueryParameter("userId", _user.Id);
        await JSRuntime.InvokeVoidAsync("history.replaceState", null, null, newUri);

        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        return [];
    }

    private async Task<IEnumerable<string>> ValidateNewPasswordAsync(EditContext context, FieldIdentifier identifier)
    {
        IEnumerable<Task<IdentityResult>> validationTasks = UserManager.PasswordValidators.Select(validator => validator.ValidateAsync(UserManager, _user!, _model.NewPassword));
        IEnumerable<IdentityResult> results = await Task.WhenAll(validationTasks).ConfigureAwait(false);

        if (results.Any(result => !result.Succeeded))
        {
            return results
                .SelectMany(result => result.Errors)
                .DistinctBy(error => error.Code)
                .Select(error => error.Description);
        }

        return [];
    }

    private Task<IEnumerable<string>> ValidateConfirmedPasswordAsync(EditContext context, FieldIdentifier identifier)
    {
        if (!_model.NewPassword.Equals(_model.ConfirmedPassword))
        {
            return Task.FromResult<IEnumerable<string>>([Localizer["confirmedPassword_DoesNotMatch"].Value]);
        }

        return Task.FromResult<IEnumerable<string>>([]);
    }

    private async Task ResetPasswordAsync()
    {
        if (_user is null || ChangeToken is null)
        {
            return;
        }

        string decodedToken = Uri.UnescapeDataString(ChangeToken);

        IdentityResult result = await UserManager.ResetPasswordAsync(_user, decodedToken, _model.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            await InvokeAsync(() => ToastService.NotifyError(result.Errors, Localizer["errorToastTitle"]));
        }
        else
        {
            await InvokeAsync(() => ToastService.NotifySuccess(Localizer["toastTitle"], Localizer["successToastMessage"]));

            Uri returnUri = NavigationManager.ToAbsoluteUri(ReturnUrl ?? "/auth/signIn");
            NavigationManager.NavigateTo(returnUri.PathAndQuery);
        }
    }
}
