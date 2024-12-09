using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SchulCloud.DbManager.Initialization;

internal class DbInitializerCheck(DbInitializer initializer) : IHealthCheck
{
    private readonly DbInitializer _initializer = initializer;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Task? task = _initializer.ExecuteTask;

        HealthCheckResult result = task switch
        {
            { IsCompletedSuccessfully: true } => HealthCheckResult.Healthy(),
            { IsFaulted: true } => HealthCheckResult.Unhealthy(task.Exception?.InnerException?.Message ?? "An unknown exception occurred.", task.Exception),
            { IsCanceled: true } => HealthCheckResult.Unhealthy("Initialization was cancelled."),
            null => HealthCheckResult.Degraded("Initialization is still in progress."),
            _ => HealthCheckResult.Degraded("Unable to determine the health status.")
        };
        return Task.FromResult(result);
    }
}
