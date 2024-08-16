using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchulCloud.Database;
using SchulCloud.Database.Models;
using SchulCloud.Database.Stores;
using SchulCloud.DbManager.HealthChecks;
using SchulCloud.DbManager.Options;
using SchulCloud.ServiceDefaults;

namespace SchulCloud.DbManager;

internal class Program
{
    static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddValidatorsFromAssemblyContaining<IDbManager>(includeInternalTypes: true);
        builder.AddServiceDefaults();

        builder.Services.AddOpenTelemetry()
            .WithTracing(traceBuilder => traceBuilder.AddSource(DbInitializer.ActivitySourceName));

        builder.AddNpgsqlDbContext<SchulCloudDbContext>("schulcloud-db");
        builder.Services
            .Configure<IdentityOptions>(builder.Configuration.GetSection("Identity"))
            .AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddUserStore<SchulCloudUserStore>()
            .AddEntityFrameworkStores<SchulCloudDbContext>();

        builder.Services
            .AddSingleton<DbInitializer>()
            .AddHostedService(provider => provider.GetRequiredService<DbInitializer>())
            .AddSingleton<DbCleaner>()
            .AddHostedService(provider => provider.GetRequiredService<DbCleaner>());

        builder.Services.AddHealthChecks()
            .AddCheck<DbInitializerCheck>($"{nameof(DbInitializer)} health check")
            .AddCheck<DbCleanerCheck>($"{nameof(DbCleaner)} health check", tags: ["live"]);

        builder.Services.AddOptions<DefaultUserOptions>()
            .Bind(builder.Configuration.GetSection("DbInitializer:DefaultUser"))
            .ValidateOnStart();
        builder.Services.AddTransient<IValidateOptions<DefaultUserOptions>, DefaultUserOptions.Validator>();

        await builder.Build()
            .MapDefaultEndpoints()
            .RunAsync();
    }
}
