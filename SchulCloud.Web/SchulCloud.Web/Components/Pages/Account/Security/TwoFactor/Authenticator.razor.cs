using BlazorBootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using QRCoder;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Models;

namespace SchulCloud.Web.Components.Pages.Account.Security.TwoFactor;

[Authorize]
[Route("/account/security/2fa/authenticator")]
public sealed partial class Authenticator : ComponentBase
{
    #region Injections
    [Inject]
    private IMemoryCache Cache { get; set; } = default!;

    [Inject]
    private IStringLocalizer<Authenticator> Localizer { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;
    #endregion

    private ApplicationUser _user = default!;
    private (string Base32Secret, string SvgRenderedQrCode)? _authenticatorInfo;
    private readonly AuthenticatorModel _model = new();

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

        _authenticatorInfo = await Cache.GetOrCreateAsync(await GetCacheKeyAsync().ConfigureAwait(false), CreateAuthenticatorInfoAsync);
    }

    private async Task<string> GetCacheKeyAsync()
    {
        string userId = await UserManager.GetUserIdAsync(_user).ConfigureAwait(false);
        return $"authenticatorData_{userId}";
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

        string userName = (await UserManager.GetUserNameAsync(_user).ConfigureAwait(false))!;
        PayloadGenerator.OneTimePassword payload = new()
        {
            Issuer = UserManager.Options.Tokens.AuthenticatorIssuer,
            Label = userName,
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
            Cache.Remove(await GetCacheKeyAsync().ConfigureAwait(false));

            await InvokeAsync(() => ToastService.NotifySuccess(Localizer["enableSuccess_Title"], Localizer["enableSuccess_Message"])).ConfigureAwait(false);
            NavigationManager.NavigateToSecurityIndex();
        }
        else
        {
            await InvokeAsync(() => ToastService.NotifyError(result.Errors, Localizer["enableError_Title"])).ConfigureAwait(false);
        }

        return [];
    }
}
