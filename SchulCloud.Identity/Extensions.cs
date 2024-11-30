using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchulCloud.Identity.Managers;
using SchulCloud.Identity.Options;
using SchulCloud.Identity.Services.Abstractions;
using SchulCloud.Store.Managers;

namespace SchulCloud.Identity;

public static class Extensions
{
    /// <summary>
    /// Configures all options that identity requires.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IHostApplicationBuilder ConfigureIdentity(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .Configure<IdentityOptions>(builder.Configuration.GetSection("Identity"))
            .Configure<EmailSenderOptions>(builder.Configuration.GetSection("Identity:EmailSender"))
            .Configure<DataProtectionTokenProviderOptions>(builder.Configuration.GetSection("Identity:TokenProviders:DataProtectionTokenProvider"))
            .Configure<AuthenticationCodeProviderOptions>(builder.Configuration.GetSection("Identity:TokenProviders:AuthenticationCodeTokenProvider"));

        builder.Services
            .Configure<IdentityFido2Options>(builder.Configuration.GetSection("Identity:Fido2"))
            .Configure<ExtendedTokenProviderOptions>(builder.Configuration.GetSection("Identity:Tokens"))
            .Configure<ApiKeyOptions>(builder.Configuration.GetSection("Identity:ApiKeys"));
        return builder;
    }

    /// <summary>
    /// Adds a required service needed to interact in any way with api keys.
    /// </summary>
    /// <typeparam name="TService">The service implementation to use.</typeparam>
    /// <param name="builder">The identity builder to use.</param>
    /// <returns>The builder pipeline</returns>
    public static IdentityBuilder AddApiKeysService<TService>(this IdentityBuilder builder)
        where TService : class, IApiKeyService
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IApiKeyService, TService>();
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="AppUserManager{TUser}"/> and <see cref="AppRoleManager{TRole}"/> to the identity builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder pipeline.</returns>
    public static IdentityBuilder AddManagers(this IdentityBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Type userManagerType = typeof(UserManager<>).MakeGenericType(builder.UserType);
        Type customUserManagerType = typeof(AppUserManager<>).MakeGenericType(builder.UserType);
        builder.Services.AddManager(userManagerType, customUserManagerType);

        if (builder.RoleType is not null)
        {
            Type roleManagerType = typeof(RoleManager<>).MakeGenericType(builder.RoleType);
            Type customRoleManagerType = typeof(AppRoleManager<>).MakeGenericType(builder.RoleType);
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
