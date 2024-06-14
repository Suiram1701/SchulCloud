using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SchulCloud.DbManager.HealthChecks;

internal class DbCleanerCheck(DbCleaner cleaner) : IHealthCheck
{
    private readonly DbCleaner _cleaner = cleaner;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Task? task = _cleaner.ExecuteTask;

        HealthCheckResult result = task switch
        {
            { IsCompleted: false } => HealthCheckResult.Healthy("Cleaner is running or awaiting its runtime."),
            { IsFaulted: true } => HealthCheckResult.Unhealthy(task.Exception?.InnerException?.Message ?? "An unknown exception occurred during cleaning.", task.Exception),
            { IsCanceled: true } => HealthCheckResult.Unhealthy("Cleaner task was cancelled."),
            { IsCompleted: true } => HealthCheckResult.Unhealthy("Cleaner task end. Unable to do more cleanup cycles."),
            _ => HealthCheckResult.Degraded("Unable to determine health status of the cleaner.")
        };
        return Task.FromResult(result);
    }
}
