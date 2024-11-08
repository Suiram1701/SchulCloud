using Fido2NetLib;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchulCloud.Store.Enums;
using SchulCloud.Store.Models;
using SchulCloud.Frontend.Services;
using SchulCloud.Frontend.Services.Interfaces;
using SchulCloud.Frontend.Services.Models;
using System.Net;
using System.Security.Claims;

namespace SchulCloud.Frontend.Identity.Managers;

/// <summary>
/// A sign in manager that provides extended sign in logic.
/// </summary>
public class SchulCloudSignInManager(
    AppUserManager userManager,
    LoginLogBackgroundService loginLogService,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    private readonly AppUserManager _userManager = userManager;

    public override async Task<SignInResult> PasswordSignInAsync(ApplicationUser user, string password, bool isPersistent, bool lockoutOnFailure)
    {
        SignInResult result = await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        await LogLoginAttemptAsync(user, result, LoginAttemptMethod.Password);

        return result;
    }

    /// <summary>
    /// Tries to sign in with a users fido2 passkey credential.
    /// </summary>
    /// <param name="options">The options used to request the fio2 credential.</param>
    /// <param name="response">The raw response of the authenticator.</param>
    /// <param name="isPersistent">Indicates whether the session is persistent.</param>
    /// <returns><c>result</c> is the result of the operation and <c>user</c> is the owner of the credential if <c>result</c> is <see cref="SignInResult.Succeeded"/>.</returns>
    public async Task<(SignInResult result, ApplicationUser? user)> Fido2PasskeySignInAsync(AssertionOptions options, AuthenticatorAssertionRawResponse response, bool isPersistent)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(response);

        UserCredential? credential = await _userManager.MakeFido2AssertionAsync(null, response, options);
        if (credential is null)
        {
            return (SignInResult.Failed, null);
        }

        // Checks whether user and credential support passkeys.
        ApplicationUser user = (await _userManager.FindUserByFido2CredentialAsync(credential))!;
        if (!await _userManager.GetPasskeySignInEnabledAsync(user) || !await _userManager.GetIsPasskey(credential))
        {
            return (SignInResult.Failed, null);
        }

        if (await PreSignInCheck(user) is SignInResult error)
        {
            return (error, null);
        }

        SignInResult result = credential is not null
            ? await SignInOrTwoFactorAsync(user, isPersistent, loginProvider: "pka", bypassTwoFactor: true)     // 'pka' means Passkey Authentication
            : SignInResult.Failed;
        await LogLoginAttemptAsync(user, result, LoginAttemptMethod.Passkey);

        return (result, user);
    }

    public override async Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient)
    {
        SignInResult result = await base.TwoFactorAuthenticatorSignInAsync(code, isPersistent, rememberClient);

        ApplicationUser user = (await GetTwoFactorAuthenticationUserAsync())!;
        await LogLoginAttemptAsync(user, result, LoginAttemptMethod.TwoFactorAuthenticator);

        return result;
    }

    /// <summary>
    /// Tries to sign in with an code sent to the user's email.
    /// </summary>
    /// <param name="code">The received code.</param>
    /// <param name="isPersistent">Indicates whether the session should be persistent.</param>
    /// <param name="rememberClient">Indicates whether this client should be remembered for future sign in attempts.</param>
    /// <returns>The verification result.</returns>
    public async Task<SignInResult> TwoFactorEmailSignInAsync(string code, bool isPersistent, bool rememberClient)
    {
        string providerName = _userManager.ExtendedTokenProviderOptions.EmailTwoFactorTokenProvider;
        SignInResult result = await TwoFactorSignInAsync(providerName, code, isPersistent, rememberClient);

        ApplicationUser user = (await GetTwoFactorAuthenticationUserAsync())!;
        await LogLoginAttemptAsync(user, result, LoginAttemptMethod.TwoFactorEmail);

        return result;
    }

    /// <summary>
    /// Tries to sign in with fido2 credential.
    /// </summary>
    /// <param name="options">The options used to request the credential from the client's authenticator.</param>
    /// <param name="response">The raw response of the client's authenticator.</param>
    /// <param name="isPersistent">Indicates whether the session is persistent.</param>
    /// <param name="rememberClient">Indicates whether this client should be remembered for future 2fa sign ins.</param>
    /// <returns>The result of the sign in.</returns>
    public async Task<SignInResult> TwoFactorFido2UserCredentialSignInAsync(AssertionOptions options, AuthenticatorAssertionRawResponse response, bool isPersistent, bool rememberClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(response);

        TwoFactorInfo? twoFactorInfo = await GetTwoFactorInfoAsync();
        if (twoFactorInfo is null)
        {
            return SignInResult.Failed;
        }

        if (await PreSignInCheck(twoFactorInfo.User) is SignInResult error)
        {
            return error;
        }

        UserCredential? credential = await _userManager.MakeFido2AssertionAsync(twoFactorInfo.User, response, options);
        SignInResult result = credential is not null
            ? await DoTwoFactorSignInAsync(twoFactorInfo, isPersistent, rememberClient)
            : SignInResult.Failed;

        await LogLoginAttemptAsync(twoFactorInfo.User, result, LoginAttemptMethod.TwoFactorSecurityKey);
        return result;
    }

    public override async Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(string recoveryCode)
    {
        SignInResult result = await base.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        ApplicationUser user = (await GetTwoFactorAuthenticationUserAsync())!;
        await LogLoginAttemptAsync(user, result, LoginAttemptMethod.TwoFactorRecoveryCode);

        return result;
    }

    private async Task<TwoFactorInfo?> GetTwoFactorInfoAsync()
    {
        // logic from SignInManager<TUser>.RetrieveTwoFactorInfoAsync
        AuthenticateResult result = await Context.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        if (result?.Principal is null)
        {
            return null;
        }

        string? userId = result.Principal.FindFirstValue(ClaimTypes.Name);
        if (userId is null)
        {
            return null;
        }

        ApplicationUser? user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        string? loginProvider = result.Principal.FindFirstValue(ClaimTypes.AuthenticationMethod);
        return new(user, loginProvider);
    }

    private async Task<SignInResult> DoTwoFactorSignInAsync(TwoFactorInfo info, bool isPersistent, bool rememberClient)
    {
        // logic from SignInManager<TUser>.DoTwoFactorSignInAsync

        if (_userManager.SupportsUserLockout)
        {
            await ResetLockout(info.User);
        }

        // Cleanup external cookie
        await Context.SignOutAsync(IdentityConstants.ExternalScheme);

        // Cleanup two factor user id cookie
        await Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

        if (rememberClient)
        {
            await RememberTwoFactorClientAsync(info.User);
        }

        List<Claim> claims = [new("amr", "mfa")];
        if (info.LoginProvider != null)
        {
            claims.Add(new(ClaimTypes.AuthenticationMethod, info.LoginProvider));
        }
        await SignInWithClaimsAsync(info.User, isPersistent, claims);

        return SignInResult.Success;
    }

    private async Task LogLoginAttemptAsync(ApplicationUser user, SignInResult result, LoginAttemptMethod method)
    {
        if (_userManager.SupportsUserLoginAttempts)
        {
            IPAddress clientIpAddress = Context.Connection.RemoteIpAddress ?? IPAddress.None;
            string? userAgent = Context.Request.Headers.UserAgent.ToString();
            LoginAttemptResult attemptResult = result switch
            {
                { Succeeded: true } => LoginAttemptResult.Succeeded,
                { RequiresTwoFactor: true } => LoginAttemptResult.TwoFactorRequired,
                { IsLockedOut: true } => LoginAttemptResult.LockedOut,
                { IsNotAllowed: true } => LoginAttemptResult.NotAllowed,
                _ => LoginAttemptResult.Failed
            };

            UserLoginAttempt attempt = new()
            {
                Method = method,
                Result = attemptResult,
                IpAddress = clientIpAddress,
                UserAgent = userAgent,
                DateTime = DateTime.UtcNow
            };
            await loginLogService.EnqueueAttemptAsync(user, attempt);
        }
    }

    private record TwoFactorInfo(ApplicationUser User, string? LoginProvider);
}
