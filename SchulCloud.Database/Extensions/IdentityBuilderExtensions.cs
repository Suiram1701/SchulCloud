using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchulCloud.Database.Stores;

namespace SchulCloud.Database.Extensions;

public static class IdentityBuilderExtensions
{
    /// <summary>
    /// Adds the stores of the <see cref="AppDbContext"/> to the identity builder.
    /// </summary>
    /// <typeparam name="TContext">The type of the db context to use.</typeparam>
    /// <param name="builder">The identity bulder.</param>
    /// <returns>The builder pipeline.</returns>
    public static IdentityBuilder AddSchulCloudEntityFrameworkStores<TContext>(this IdentityBuilder builder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        Type userStoreType = typeof(SchulCloudUserStore<,,>).MakeGenericType(
            builder.UserType,
            builder.RoleType ?? typeof(IdentityRole),
            typeof(TContext));
        builder.Services.AddScoped(typeof(IUserStore<>).MakeGenericType(builder.UserType), userStoreType);

        if (builder.RoleType is not null)
        {
            Type roleStoreType = typeof(SchulCloudRoleStore<,>).MakeGenericType(
                builder.RoleType,
                typeof(TContext));
            builder.Services.AddScoped(typeof(IRoleStore<>).MakeGenericType(builder.RoleType), roleStoreType);
        }

        return builder;
    }
}
