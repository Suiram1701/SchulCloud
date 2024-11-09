using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SchulCloud.Authorization.Requirements;
using System.Reflection;

namespace SchulCloud.Authorization.Extensions;

public static class AuthorizationBuilderExtensions
{
    /// <summary>
    /// Adds the policies that are provided by the permissions.
    /// </summary>
    /// <param name="builder">The builder to use.</param>
    /// <returns>The builder pipeline.</returns>
    public static AuthorizationBuilder AddPermissionsPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        foreach (FieldInfo fieldInfo in typeof(Permissions).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            string permissionName = fieldInfo.GetValue(null) as string
                ?? throw new InvalidOperationException("Unable to get permissions name.");

            foreach (PermissionLevel level in new[] { PermissionLevel.Read, PermissionLevel.Write, PermissionLevel.Special })
            {
                builder.AddPolicy($"{permissionName}-{level}", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new PermissionRequirement(permissionName, level));
                });
            }
        }

        return builder;
    }
}
