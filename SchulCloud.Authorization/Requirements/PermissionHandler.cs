using Microsoft.AspNetCore.Authorization;
using System.Security;
using System.Security.Claims;

namespace SchulCloud.Authorization.Requirements;

internal class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        IEnumerable<Claim> permissionClaims = context.User.FindAll(ClaimTypes.Permission);
        bool authorizedResult = permissionClaims.Any(claim =>
        {
            Permission permission = Permission.Parse(claim.Value);
            return permission.Name == requirement.Name && permission.Level >= requirement.PermissionLevel;
        });

        if (authorizedResult)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new(this, "User does not have the required permission."));
        }

        return Task.CompletedTask;
    }
}