using Microsoft.AspNetCore.Localization;
using System.Security.Claims;

namespace SchulCloud.Frontend.RequestCultureProviders;

/// <summary>
/// A request culture provider that reads the culture from claims of an authenticated user.
/// </summary>
/// <param name="anonymousProvider">If the user isn't authenticated this provider will be used.</param>
public class ClaimsCultureProvider(IRequestCultureProvider? anonymousProvider = null) : IRequestCultureProvider
{
    public const string CultureClaimType = "Setting:Culture";
    public const string UiCultureClaimType = "Setting:UiCulture";

    public async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated ?? false)
        {
            Claim? cultureClaim = httpContext.User.Claims.SingleOrDefault(claim => claim.Type == CultureClaimType);
            Claim? uiCultureClaim = httpContext.User.Claims.SingleOrDefault(claim => claim.Type == UiCultureClaimType);
            if (cultureClaim is null || uiCultureClaim is null)
                return null;

            return new(culture: cultureClaim.Value, uiCulture: uiCultureClaim.Value);
        }
        else if (anonymousProvider is not null)
        {
            return await anonymousProvider.DetermineProviderCultureResult(httpContext);
        }
        else
        {
            return null;
        }
    }
}
