using Quartz;
using SchulCloud.DbManager.Quartz;
using SchulCloud.ServiceDefaults.Services;
using System.Diagnostics;

namespace SchulCloud.DbManager;

internal static class Commands
{
    public static void MapDbManagerCommands(IEndpointRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.MapGet("/cleanup", async (HttpContext context, ISchedulerFactory schedulerFactory) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);

            JobDataMap jobData = [new KeyValuePair<string, object?>("trigger", Activity.Current?.Context)];
            await scheduler.TriggerJob(Jobs.CleanerJob, jobData, context.RequestAborted).ConfigureAwait(false);
        });
        builder.MapGet("/cleanup-pause", async (HttpContext context, ISchedulerFactory schedulerFactory) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);
            await scheduler.PauseTrigger(Jobs.CleanerJobTimeTrigger, context.RequestAborted).ConfigureAwait(false);
        });
        builder.MapGet("/cleanup-resume", async (HttpContext context, ISchedulerFactory schedulerFactory) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);
            await scheduler.ResumeTrigger(Jobs.CleanerJobTimeTrigger, context.RequestAborted).ConfigureAwait(false);
        });

        builder.MapGet("/initialize-db", async (HttpContext context, ISchedulerFactory schedulerFactory, IDataManager manager) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);

            JobDataMap jobData = [new KeyValuePair<string, object?>("trigger", Activity.Current?.Context)];
            await scheduler.TriggerJob(Jobs.InitializerJob, jobData, context.RequestAborted).ConfigureAwait(false);
        });
        builder.MapGet("/drop-db", async (HttpContext context, ISchedulerFactory schedulerFactory, IDataManager manager) =>
        {
            IScheduler scheduler = await schedulerFactory.GetScheduler(context.RequestAborted).ConfigureAwait(false);
            await scheduler.PauseJob(Jobs.CleanerJob).ConfigureAwait(false);

            await manager.RemoveDataSourceAsync(context.RequestAborted).ConfigureAwait(false);
        });
    }
}
