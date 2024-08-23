using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchulCloud.Store.Managers;

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
    /// <summary>
    /// Tries to sign in with an code sent to the user's email.
    /// </summary>
    /// <param name="code">The received code.</param>
    /// <param name="isPersistent">Indicates whether the session should be persistent.</param>
    /// <param name="rememberClient">Indicates whether this client should be remembered for future sign in attempts.</param>
    /// <returns>The verification result.</returns>
    public async Task<SignInResult> TwoFactorEmailSignInAsync(string code, bool isPersistent, bool rememberClient)
    {
        string providerName = userManager.ExtendedTokenProviderOptions.EmailTwoFactorTokenProvider;
        return await TwoFactorSignInAsync(providerName, code, isPersistent, rememberClient);
    }
}
