using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Store.Options;

/// <summary>
/// Extended options for <see cref="TokenOptions"/>
/// </summary>
public class ExtendedTokenProviderOptions
{
    /// <summary>
    /// The provider name of <see cref="Identity.TokenProviders.AuthenticationCodeTokenProvider{TUser}"/>.
    /// </summary>
    public static readonly string AuthenticationTokenProvider = "AuthenticationToken";

    /// <summary>
    /// The name of the provider used for email 2FA.
    /// </summary>
    public string EmailTwoFactorTokenProvider { get; set; } = AuthenticationTokenProvider;
}
