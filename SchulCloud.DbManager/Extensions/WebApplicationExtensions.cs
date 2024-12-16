using Quartz;
using SchulCloud.DbManager.Quartz;
using SchulCloud.ServiceDefaults.Services;
using System.Diagnostics;

namespace SchulCloud.DbManager.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapCommands(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/commands/cleanup", async (HttpContext context, ISchedulerFactory schedulerFactory) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);

            JobDataMap jobData = [new KeyValuePair<string, object?>("trigger", Activity.Current?.Context)];
            await scheduler.TriggerJob(Jobs.CleanerJob, jobData, context.RequestAborted).ConfigureAwait(false);
        });
        app.MapGet("/commands/cleanup-pause", async (HttpContext context, ISchedulerFactory schedulerFactory) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);
            await scheduler.PauseTrigger(Jobs.CleanerJobTimeTrigger, context.RequestAborted).ConfigureAwait(false);
        });
        app.MapGet("/commands/cleanup-resume", async (HttpContext context, ISchedulerFactory schedulerFactory) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);
            await scheduler.ResumeTrigger(Jobs.CleanerJobTimeTrigger, context.RequestAborted).ConfigureAwait(false);
        });

        app.MapGet("/commands/initialize-db", async (HttpContext context, ISchedulerFactory schedulerFactory, IDataManager manager) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);

            JobDataMap jobData = [new KeyValuePair<string, object?>("trigger", Activity.Current?.Context)];
            await scheduler.TriggerJob(Jobs.InitializerJob, jobData, context.RequestAborted).ConfigureAwait(false);
        });
        app.MapGet("/commands/drop-db", async (HttpContext context, ISchedulerFactory schedulerFactory, IDataManager manager) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);
            await scheduler.PauseJob(Jobs.CleanerJob).ConfigureAwait(false);

            await manager.RemoveDataSourceAsync(context.RequestAborted).ConfigureAwait(false);
        });

        return app;
    }
}
