using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using MudBlazor;
using QRCoder;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Models;

namespace SchulCloud.Web.Components.Pages.Account.Security.TwoFactor;

[Route("/account/security/twoFactor/authenticator")]
public sealed partial class Authenticator : ComponentBase
{
    #region Injections
    [Inject]
    private IMemoryCache Cache { get; set; } = default!;

    [Inject]
    private IStringLocalizer<Authenticator> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private MudForm _form = default!;

    private ApplicationUser _user = default!;
    private (string Base32Secret, string SvgRenderedQrCode)? _authenticatorInfo;
    private readonly AuthenticatorModel _model = new();

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState;
        _user = (await UserManager.GetUserAsync(authenticationState.User))!;

        if (await UserManager.GetTwoFactorEnabledAsync(_user))
        {
            NavigationManager.NavigateToSecurityIndex();
            return;
        }

        _authenticatorInfo = await Cache.GetOrCreateAsync(await GetCacheKeyAsync(), CreateAuthenticatorInfoAsync);
    }

    private async Task Form_IsValidChanged(bool valid)
    {
        if (!valid)
        {
            return;
        }

        IdentityResult enableResult = await UserManager.SetTwoFactorEnabledAsync(_user, true);
        if (enableResult.Succeeded)
        {
            Cache.Remove(await GetCacheKeyAsync());

            SnackbarService.AddSuccess(Localizer["enableSuccess"]);
            NavigationManager.NavigateToSecurityIndex();
        }
        else
        {
            SnackbarService.AddError(enableResult.Errors, Localizer["enableError"]);
        }
    }

    private async Task<string?> Form_ValidateCodeAsync(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Localizer["form_NotEmpty"];
        }

        string tokenProvider = UserManager.Options.Tokens.AuthenticatorTokenProvider;
        if (!await UserManager.VerifyTwoFactorTokenAsync(_user, tokenProvider, value.Replace(" ", "")))
        {
            return Localizer["form_CodeInvalid"];
        }
        return null;
    }

    private async Task<string> GetCacheKeyAsync()
    {
        string userId = await UserManager.GetUserIdAsync(_user);
        return $"authenticatorEnableData_{userId}";
    }

    private async Task<(string, string)?> CreateAuthenticatorInfoAsync(ICacheEntry entry)
    {
        IdentityResult result = await UserManager.ResetAuthenticatorKeyAsync(_user);
        if (!result.Succeeded)
        {
            entry.SlidingExpiration = TimeSpan.Zero;

            SnackbarService.AddError(result.Errors, Localizer["requestError"]);
            return null;
        }

        entry.SlidingExpiration = TimeSpan.FromMinutes(10);
        string base32secret = (await UserManager.GetAuthenticatorKeyAsync(_user))!;

        string userName = (await UserManager.GetUserNameAsync(_user))!;
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
}
