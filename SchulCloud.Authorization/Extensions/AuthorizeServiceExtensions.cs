using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SchulCloud.Authorization.Extensions;

public static class AuthorizeServiceExtensions
{
    /// <summary>
    /// Checks whether a user have a permission with a minimum level.
    /// </summary>
    /// <param name="authorizationService">The service to use.</param>
    /// <param name="user">The user to check this for.</param>
    /// <param name="name">The required permission.</param>
    /// <param name="level">The minimum level of the permission.</param>
    /// <returns>The result of this check.</returns>
    public static async Task<AuthorizationResult> RequirePermissionAsync(this IAuthorizationService authorizationService, ClaimsPrincipal user, string name, PermissionLevel level)
    {
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await authorizationService.AuthorizeAsync(user, $"{name}-{level}");
    }
}
