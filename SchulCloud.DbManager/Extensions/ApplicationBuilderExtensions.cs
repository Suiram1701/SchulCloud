using Microsoft.AspNetCore.Identity;
using SchulCloud.Store;

namespace SchulCloud.DbManager.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IHostApplicationBuilder ConfigureOptions(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection("Identity"));
        builder.ConfigureManagers();
        return builder;
    }
}
