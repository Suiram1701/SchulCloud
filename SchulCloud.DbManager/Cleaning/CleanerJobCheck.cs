using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;
using SchulCloud.DbManager.Quartz;
using System.Reflection.Metadata.Ecma335;

namespace SchulCloud.DbManager.Cleaning;

internal class CleanerJobCheck(ISchedulerFactory schedulerFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken).ConfigureAwait(false);        
        if (await scheduler.CheckExists(Jobs.CleanerJob, cancellationToken).ConfigureAwait(false))
        {
            return await scheduler.GetTriggerState(Jobs.CleanerJobTimeTrigger, cancellationToken).ConfigureAwait(false) switch
            {
                TriggerState.Normal => HealthCheckResult.Healthy(),
                TriggerState.Paused => HealthCheckResult.Degraded("Cleaner manually paused."),
                TriggerState.Blocked => HealthCheckResult.Degraded("Cleaner blocked due concurrent execution attempt."),
                TriggerState.None => HealthCheckResult.Unhealthy("Cleaner job registered while automated trigger not."),
                TriggerState.Error => HealthCheckResult.Unhealthy("Cleaner job won't be executed during until manual intervention."),
                _ => HealthCheckResult.Unhealthy("Unable to determine the health state of the cleaner.")
            };
        }
        else
        {
            return HealthCheckResult.Unhealthy("Cleaner job isn't registered.");
        }
    }
}
