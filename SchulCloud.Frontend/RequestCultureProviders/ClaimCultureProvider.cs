using Microsoft.AspNetCore.Localization;
using System.Security.Claims;

namespace SchulCloud.Frontend.RequestCultureProviders;

public class ClaimCultureProvider : IRequestCultureProvider
{
    public const string CultureClaimType = "Setting:Culture";
    public const string UiCultureClaimType = "Setting:UiCulture";

    public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (!(httpContext.User.Identity?.IsAuthenticated ?? false))
            return Task.FromResult<ProviderCultureResult?>(null);

        Claim? cultureClaim = httpContext.User.Claims.SingleOrDefault(claim => claim.Type == CultureClaimType);
        Claim? uiCultureClaim = httpContext.User.Claims.SingleOrDefault(claim => claim.Type == UiCultureClaimType);
        if (cultureClaim is null || uiCultureClaim is null)
            return Task.FromResult<ProviderCultureResult?>(null);

        ProviderCultureResult result = new(culture: cultureClaim.Value, uiCulture: uiCultureClaim.Value);
        return Task.FromResult<ProviderCultureResult?>(result);
    }
}
