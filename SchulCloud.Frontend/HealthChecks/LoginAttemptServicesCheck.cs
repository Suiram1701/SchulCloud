using Microsoft.Extensions.Diagnostics.HealthChecks;
using SchulCloud.Frontend.HostedServices;
using System.Threading.Tasks;

namespace SchulCloud.Frontend.HealthChecks;

/// <summary>
/// A health check that verifies that every hosted service is running.
/// </summary>
/// <param name="attemptLoggingService"></param>
internal class LoginAttemptServicesCheck(LoginAttemptLoggingService attemptLoggingService) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Task? task = attemptLoggingService.ExecuteTask;

        HealthCheckResult result = task switch
        {
            { IsCompleted: false } => HealthCheckResult.Healthy(),
            { IsFaulted: true } => HealthCheckResult.Unhealthy(task.Exception?.InnerException?.Message ?? "An unknown exception occurred.", task.Exception),
            { IsCanceled: true } => HealthCheckResult.Unhealthy("Service was cancelled."),
            { IsCompleted: true } => HealthCheckResult.Unhealthy("Service ended. Unable to proceed any data anymore."),
            _ => HealthCheckResult.Degraded("Unable to determine health status.")
        };
        return Task.FromResult(result);
    }
}
