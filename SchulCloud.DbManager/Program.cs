using FluentValidation;
using SchulCloud.Database;
using SchulCloud.Database.Extensions;
using SchulCloud.DbManager.Cleaning;
using SchulCloud.DbManager.Extensions;
using SchulCloud.DbManager.Initialization;
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
            .ConfigureIdentity()
            .ConfigureOptions();
        builder.Services.AddValidatorsFromAssemblyContaining<IDbManager>(includeInternalTypes: true);

        builder.AddAspirePostgresDb<AppDbContext>(ResourceNames.IdentityDatabase);
        builder.Services.AddDataManager<DatabaseManager>();

        builder.Services.AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddSchulCloudEntityFrameworkStores<AppDbContext>()
            .AddManagers();

        builder.Services
            .AddSingleton<DbInitializer>()
            .AddHostedService(provider => provider.GetRequiredService<DbInitializer>())
            .AddSingleton<DbCleaner>()
            .AddHostedService(provider => provider.GetRequiredService<DbCleaner>());

        builder.Services.AddHealthChecks()
            .AddCheck<DbInitializerCheck>("DbInitializer")
            .AddCheck<DbCleanerCheck>($"DbCleaner");

        builder.Services.AddOpenTelemetry()
            .WithMetrics(options => options.AddMeter(DbCleaner.MeterName))
            .WithTracing(options => options.AddSource(DbInitializer.ActivitySourceName, DbCleaner.ActivitySourceName));

        await builder.Build()
            .MapDefaultEndpoints()
            .MapCommands()
            .RunAsync();
    }
}
