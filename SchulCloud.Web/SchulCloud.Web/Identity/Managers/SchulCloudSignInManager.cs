using Fido2NetLib;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchulCloud.Store.Managers;
using System.Security.Claims;

namespace SchulCloud.Web.Identity.Managers;

/// <summary>
/// A sign in manager that provides extended sign in logic.
/// </summary>
public class SchulCloudSignInManager<TUser, TCredential>(
    SchulCloudUserManager<TUser, TCredential> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<TUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<TUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<TUser> confirmation)
    : SignInManager<TUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    where TUser : class
    where TCredential : class
{
    private readonly SchulCloudUserManager<TUser, TCredential> _userManager = userManager;

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
        return await TwoFactorSignInAsync(providerName, code, isPersistent, rememberClient);
    }

    /// <summary>
    /// Tries to sign in with fido2 credential.
    /// </summary>
    /// <param name="options">The options used to request the credential from the client's authenticator.</param>
    /// <param name="response">The raw response of the client's authenticator.</param>
    /// <param name="isPersistent">Indicates whether the session is persistent.</param>
    /// <param name="rememberClient">Indicates whether this client should be remembered for future 2fa sign ins.</param>
    /// <returns>The result of the sign in.</returns>
    public async Task<SignInResult> TwoFactorFido2CredentialSignInAsync(AssertionOptions options, AuthenticatorAssertionRawResponse response, bool isPersistent, bool rememberClient)
    {
        TwoFactorInfo? twoFactorInfo = await GetTwoFactorInfoAsync().ConfigureAwait(false);
        if (twoFactorInfo is null)
        {
            return SignInResult.Failed;
        }

        if (await PreSignInCheck(twoFactorInfo.User).ConfigureAwait(false) is SignInResult error)
        {
            return error;
        }

        (TUser _, TCredential credential)? assertionResult = await _userManager.MakeFido2AssertionAsync(twoFactorInfo.User, response, options).ConfigureAwait(false);
        if (assertionResult is null)
        {
            return SignInResult.Failed;
        }

        return await DoTwoFactorSignInAsync(twoFactorInfo, isPersistent, rememberClient).ConfigureAwait(false);
    }

    private async Task<TwoFactorInfo?> GetTwoFactorInfoAsync()
    {
        // logic from UserManager<TUser>.RetrieveTwoFactorInfoAsync
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

        TUser? user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        string? loginProvider = result.Principal.FindFirstValue(ClaimTypes.AuthenticationMethod);
        return new(user, loginProvider);
    }

    private async Task<SignInResult> DoTwoFactorSignInAsync(TwoFactorInfo info, bool isPersistent, bool rememberClient)
    {
        // logic from UserManager<TUser>.DoTwoFactorSignInAsync

        if (_userManager.SupportsUserLockout)
        {
            await ResetLockout(info.User).ConfigureAwait(false);
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

    private record TwoFactorInfo(TUser User, string? LoginProvider);
}
