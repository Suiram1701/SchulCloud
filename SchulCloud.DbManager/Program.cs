using Microsoft.AspNetCore.Identity;
using SchulCloud.Database;
using SchulCloud.Database.Models;
using SchulCloud.DbManager.HealthChecks;
using SchulCloud.DbManager.Options;
using SchulCloud.DbManager.Options.Validators;
using SchulCloud.ServiceDefaults;

namespace SchulCloud.DbManager;

internal class Program
{
    static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddOpenTelemetry()
            .WithTracing(options =>
            {
                options.AddSource(DbInitializer.ActivitySourceName);
            });

        builder.AddNpgsqlDbContext<SchulCloudDbContext>("schulcloud-db");
        builder.Services.AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddEntityFrameworkStores<SchulCloudDbContext>();

        builder.Services.AddSingleton<DbInitializer>();
        builder.Services.AddSingleton<DbCleaner>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<DbInitializer>());
        builder.Services.AddHostedService(provider => provider.GetRequiredService<DbCleaner>());

        builder.Services.AddHealthChecks()
            .AddCheck<DbInitializerCheck>($"{nameof(DbInitializer)} health check")
            .AddCheck<DbCleanerCheck>($"{nameof(DbCleaner)} health check", tags: [ "live" ]);

        IConfigurationSection initializerSection = builder.Configuration.GetSection("DbInitializer");
        builder.Services.AddOptionsWithValidateOnStart<DefaultUserOptions, DefaultUserValidator>()
            .Bind(initializerSection.GetSection("DefaultUser"));

        await builder.Build()
            .MapDefaultEndpoints()
            .RunAsync();
    }
}
