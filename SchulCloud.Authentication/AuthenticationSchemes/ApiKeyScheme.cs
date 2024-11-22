using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SchulCloud.Authorization;
using SchulCloud.Store.Managers;
using SchulCloud.Store.Models;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SchulCloud.Authentication.AuthenticationSchemes;

internal class ApiKeyScheme<TUser>(
    IMemoryCache cache,
    IProblemDetailsService problemDetailsService,
    IOptionsMonitor<ApiKeySchemeOptions> options,
    ILoggerFactory logger,
    SchulCloudUserManager<TUser> userManager,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeySchemeOptions>(options, logger, encoder)
    where TUser : class
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue(HeaderNames.ApiKeyHeader, out StringValues headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        if (!userManager.SupportsUserApiKeys)
        {
            return AuthenticateResult.Fail($"{userManager.GetType()} does not support API keys.");
        }

        string providedKey = headerValues.ToString();
        (UserApiKey ApiKey, TUser User)? result = await cache.GetOrCreateAsync(GetCacheKey(providedKey), async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
            return await userManager.FindApiKeyAsync(providedKey).ConfigureAwait(false);
        }).ConfigureAwait(false);

        if (result is not null)
        {
            ClaimsIdentity identity = await GenerateClaimsAsync(result.Value.User, result.Value.ApiKey).ConfigureAwait(false);
            ClaimsPrincipal principal = new(identity);

            return AuthenticateResult.Success(new(principal, SchemeNames.ApiKeyScheme));
        }
        else
        {
            return AuthenticateResult.NoResult();
        }
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await problemDetailsService.WriteAsync(new()
        {
            HttpContext = Context,
            ProblemDetails = new()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized),
                Status = StatusCodes.Status401Unauthorized,
                Detail = "An API key is required to call this endpoint.",
            }
        }).ConfigureAwait(false);
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await problemDetailsService.WriteAsync(new()
        {
            HttpContext = Context,
            ProblemDetails = new()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status403Forbidden),
                Status = StatusCodes.Status403Forbidden,
                Detail = "The used API key does not have the privileges to call this endpoint.",
            }
        }).ConfigureAwait(false);
    }

    private async Task<ClaimsIdentity> GenerateClaimsAsync(TUser user, UserApiKey apiKey)
    {
        ClaimsIdentityOptions claimsOptions = userManager.Options.ClaimsIdentity;

        string userId = await userManager.GetUserIdAsync(user).ConfigureAwait(false);
        string? userName = await userManager.GetUserNameAsync(user).ConfigureAwait(false);

        ClaimsIdentity identity = new(SchemeNames.ApiKeyScheme, claimsOptions.UserNameClaimType, claimsOptions.RoleClaimType);
        identity.AddClaim(new(claimsOptions.UserIdClaimType, userId));
        identity.AddClaim(new(claimsOptions.UserNameClaimType, userName!));

        IEnumerable<Permission> permissions = apiKey.AllPermissions
            ? await userManager.GetPermissionLevelsAsync(user).ConfigureAwait(false)
            : apiKey.PermissionLevels;
        foreach (Permission permission in permissions)
        {
            identity.AddClaim(new(Authorization.ClaimTypes.Permission, permission.ToString()));
        }

        return identity;
    }

    private static string GetCacheKey(string apiKey) => $"api-key-auth-{apiKey}";
}
