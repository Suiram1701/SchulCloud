using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchulCloud.Store.Managers;
using SchulCloud.Store.Options;

namespace SchulCloud.Store;

public static class Extensions
{
    /// <summary>
    /// Configures options required for the managers.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IHostApplicationBuilder ConfigureManagers(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .Configure<IdentityFido2Options>(builder.Configuration.GetSection("Identity:Fido2"))
            .Configure<ExtendedTokenProviderOptions>(builder.Configuration.GetSection("Identity:Tokens"))
            .Configure<ApiKeyOptions>(builder.Configuration.GetSection("Identity:ApiKeys"));
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="SchulCloudUserManager{TUser}"/> and <see cref="SchulCloudRoleManager{TRole}"/> to the identity builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder pipeline.</returns>
    public static IdentityBuilder AddSchulCloudManagers(this IdentityBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Type userManagerType = typeof(UserManager<>).MakeGenericType(builder.UserType);
        Type customUserManagerType = typeof(SchulCloudUserManager<>).MakeGenericType(builder.UserType);
        builder.Services.AddManager(userManagerType, customUserManagerType);

        if (builder.RoleType is not null)
        {
            Type roleManagerType = typeof(RoleManager<>).MakeGenericType(builder.RoleType);
            Type customRoleManagerType = typeof(SchulCloudRoleManager<>).MakeGenericType(builder.RoleType);
            builder.Services.AddManager(roleManagerType, customRoleManagerType);
        }

        return builder;
    }

    private static void AddManager(this IServiceCollection services, Type managerType, Type customType)
    {
        if (!managerType.IsAssignableFrom(customType))
        {
            throw new InvalidOperationException($"Custom manager {customType.FullName} have to inherit from {managerType.FullName}.");
        }
        if (managerType != customType)
        {
            services.AddScoped(customType, provider => provider.GetRequiredService(managerType));
        }

        services.AddScoped(managerType, customType);
    }
}
