using Microsoft.Extensions.Options;
using SchulCloud.DbManager.Options;

namespace SchulCloud.DbManager.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder ConfigureOptions(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<DefaultUserOptions>()
            .Bind(builder.Configuration.GetSection("Initializer:DefaultUser"))
            .ValidateOnStart();
        builder.Services.AddTransient<IValidateOptions<DefaultUserOptions>, DefaultUserOptions.Validator>();

        builder.Services.Configure<CleanerOptions>(builder.Configuration.GetSection("Cleaner"));

        return builder;
    }
}
