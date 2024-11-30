using FluentValidation;
using Microsoft.Extensions.Options;
using SchulCloud.Database;
using SchulCloud.Database.Extensions;
using SchulCloud.DbManager.Extensions;
using SchulCloud.DbManager.HealthChecks;
using SchulCloud.DbManager.Options;
using SchulCloud.DbManager.Services;
using SchulCloud.Identity;
using SchulCloud.ServiceDefaults;

namespace SchulCloud.DbManager;

internal class Program
{
    static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder
            .AddServiceDefaults()
            .ConfigureIdentity();
        builder.Services.AddValidatorsFromAssemblyContaining<IDbManager>(includeInternalTypes: true);

        builder.Services.AddOpenTelemetry()
            .WithTracing(traceBuilder => traceBuilder.AddSource(DbInitializer.ActivitySourceName));

        builder.AddAspirePostgresDb<AppDbContext>(ResourceNames.IdentityDatabase);
        builder.Services.AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddSchulCloudEntityFrameworkStores<AppDbContext>()
            .AddSchulCloudManagers();

        builder.Services
            .AddSingleton<DbInitializer>()
            .AddHostedService(provider => provider.GetRequiredService<DbInitializer>())
            .AddSingleton<DbCleaner>()
            .AddHostedService(provider => provider.GetRequiredService<DbCleaner>());

        builder.Services.AddHealthChecks()
            .AddCheck<DbInitializerCheck>("DbInitializer")
            .AddCheck<DbCleanerCheck>($"DbCleaner");

        builder.Services.AddOptions<DefaultUserOptions>()
            .Bind(builder.Configuration.GetSection("DbInitializer:DefaultUser"))
            .ValidateOnStart();
        builder.Services.AddTransient<IValidateOptions<DefaultUserOptions>, DefaultUserOptions.Validator>();

        await builder.Build()
            .MapDefaultEndpoints()
            .MapCommands()
            .RunAsync();
    }
}
