using Microsoft.Extensions.Options;
using Quartz;
using SchulCloud.DbManager.Cleaning;
using SchulCloud.DbManager.Initialization;
using SchulCloud.DbManager.Options;

namespace SchulCloud.DbManager.Quartz;

public class ConfigureQuartz(IOptions<CleanerOptions> cleanerOptions) : IConfigureOptions<QuartzOptions>
{
    private CleanerOptions CleanerOptions => cleanerOptions.Value;

    public void Configure(QuartzOptions options)
    {
        options.AddJob<CleanerJob>(configure => configure
            .WithIdentity(Jobs.CleanerJob)
            .WithDescription("A job that cleans up the data source from obsolete and expired records.")
            .DisallowConcurrentExecution()
        );
        options.AddTrigger(trigger => trigger
            .ForJob(Jobs.CleanerJob)
            .WithIdentity(Jobs.CleanerJobTimeTrigger)
            .WithDescription("Runs the job in a specified interval.")
            .WithSimpleSchedule(scheduler => scheduler
                .WithInterval(CleanerOptions.Interval)
                .RepeatForever()
            )
        );

        options.AddJob<InitializerJob>(configure => configure
            .WithIdentity(Jobs.InitializerJob)
            .WithDescription("A job that initializes the database with the recent migration and a default user.")
            .StoreDurably()
            .DisallowConcurrentExecution()
        );
        options.AddTrigger(trigger => trigger
            .ForJob(Jobs.InitializerJob)
            .WithIdentity(Jobs.InitializerJobStartTrigger)
            .WithDescription("Runs the job on application start.")
        );
    }
}
