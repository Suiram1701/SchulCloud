using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SchulCloud.DbManager.Cleaning;

internal class DbCleanerCheck(DbCleaner cleaner) : IHealthCheck
{
    private readonly DbCleaner _cleaner = cleaner;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Task? task = _cleaner.ExecuteTask;

        HealthCheckResult result = task switch
        {
            { IsCompleted: false } => HealthCheckResult.Healthy(),
            { IsFaulted: true } => HealthCheckResult.Unhealthy(task.Exception?.InnerException?.Message ?? "An unknown exception occurred.", task.Exception),
            { IsCanceled: true } => HealthCheckResult.Unhealthy("Service was cancelled."),
            { IsCompleted: true } => HealthCheckResult.Unhealthy("Service ended. Unable to do more cleanup cycles."),
            _ => HealthCheckResult.Degraded("Unable to determine health status.")
        };
        return Task.FromResult(result);
    }
}
