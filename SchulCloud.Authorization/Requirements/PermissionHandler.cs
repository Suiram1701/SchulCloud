using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SchulCloud.Authorization.Requirements;

internal class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        IEnumerable<Claim> permissionClaims = context.User.FindAll(ClaimTypes.Permission);
        bool result = permissionClaims.Any(claim =>
        {
            string[] values = claim.Value.Split(':', 2);
            if (values[0] == requirement.Name)
            {
                PermissionLevel level = Enum.Parse<PermissionLevel>(values[1]);
                return level >= requirement.PermissionLevel;
            }
            
            return false;
        });

        if (result)
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