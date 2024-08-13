using BlazorBootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using QRCoder;
using SchulCloud.Database.Models;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Models;
using SchulCloud.Web.Options;

namespace SchulCloud.Web.Components.Pages.Account.Security;

[Authorize]
[Route("/account/security/activateAuthenticator")]
public sealed partial class ActivateAuthenticator : ComponentBase
{
    #region Injections
    [Inject]
    private IMemoryCache Cache { get; set; } = default!;

    [Inject]
    private IStringLocalizer<ActivateAuthenticator> Localizer { get; set; } = default!;

    [Inject]
    private UserManager<User> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;
    #endregion

    private User _user = default!;
    private (string Base32Secret, string SvgRenderedQrCode)? _authenticatorInfo;
    private readonly AuthenticatorModel _model = new();

    private string CacheKey => $"authenticatorActivationData_{_user.Id}";

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState.ConfigureAwait(false);
        _user = (await UserManager.GetUserAsync(authenticationState.User).ConfigureAwait(false))!;

        if (await UserManager.GetTwoFactorEnabledAsync(_user).ConfigureAwait(false))
        {
            NavigationManager.NavigateToSecurityIndex();
            return;
        }

        _authenticatorInfo = await Cache.GetOrCreateAsync(CacheKey, CreateAuthenticatorInfoAsync);
    }

    private async Task<(string, string)?> CreateAuthenticatorInfoAsync(ICacheEntry entry)
    {
        IdentityResult result = await UserManager.ResetAuthenticatorKeyAsync(_user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            entry.SlidingExpiration = TimeSpan.Zero;

            await InvokeAsync(() => ToastService.NotifyError(result.Errors, Localizer["requestError_Title"]));
            return null;
        }

        entry.SlidingExpiration = TimeSpan.FromMinutes(10);
        string base32secret = (await UserManager.GetAuthenticatorKeyAsync(_user).ConfigureAwait(false))!;

        PayloadGenerator.OneTimePassword payload = new()
        {
            Issuer = UserManager.Options.Tokens.AuthenticatorIssuer,
            Label = _user.UserName,
            Secret = base32secret,
            Type = PayloadGenerator.OneTimePassword.OneTimePasswordAuthType.TOTP
        };
        using QRCodeData data = QRCodeGenerator.GenerateQrCode(payload);
        using SvgQRCode svgQRCode = new(data);
        return (base32secret, svgQRCode.GetGraphic(6));
    }

    private async Task<IEnumerable<string>> ValidateCodeAsync(EditContext context, FieldIdentifier identifier)
    {
        string tokenProvider = UserManager.Options.Tokens.AuthenticatorTokenProvider;
        if (string.IsNullOrWhiteSpace(_model.Code) || !await UserManager.VerifyTwoFactorTokenAsync(_user, tokenProvider, _model.TrimmedCode).ConfigureAwait(false))
        {
            return [Localizer["form_CodeInvalid"]];
        }

        IdentityResult result = await UserManager.SetTwoFactorEnabledAsync(_user, true).ConfigureAwait(false);
        if (result.Succeeded)
        {
            Cache.Remove(CacheKey);

            await InvokeAsync(() => ToastService.NotifySuccess(Localizer["activationSuccess_Title"], Localizer["activationSuccess_Message"])).ConfigureAwait(false);
            NavigationManager.NavigateToSecurityIndex();
        }
        else
        {
            await InvokeAsync(() => ToastService.NotifyError(result.Errors, Localizer["activationError_Title"])).ConfigureAwait(false);
        }

        return [];
    }
}
