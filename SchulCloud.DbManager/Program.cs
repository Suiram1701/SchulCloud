using FluentValidation;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Quartz;
using Quartz.AspNetCore;
using SchulCloud.Database;
using SchulCloud.Database.Extensions;
using SchulCloud.DbManager.Cleaning;
using SchulCloud.DbManager.Extensions;
using SchulCloud.DbManager.Initialization;
using SchulCloud.DbManager.Quartz;
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

        builder.Services.AddQuartz();
        builder.Services.AddQuartzServer(options => options.WaitForJobsToComplete = true);
        builder.Services.AddTransient<IConfigureOptions<QuartzOptions>, ConfigureQuartz>();

        builder.Services.AddHealthChecks()
            .AddCheck<CleanerJobCheck>($"cleanerJob");

        builder.Services.AddOpenTelemetry()
            .WithMetrics(builder => builder.AddMeter(CleanerJob.MeterName))
            .WithTracing(builder => builder
                .AddQuartzInstrumentation(options => options.RecordException = true)
                .AddSource(InitializerJob.ActivitySourceName, CleanerJob.ActivitySourceName)
            );

        await builder.Build()
            .MapDefaultEndpoints(commandsBuilder: Commands.MapDbManagerCommands)
            .RunAsync();
    }
}
