using Microsoft.Extensions.Diagnostics.HealthChecks;
using SchulCloud.Frontend.HostedServices;
using System.Threading.Tasks;

namespace SchulCloud.Frontend.HealthChecks;

/// <summary>
/// A health check that verifies that every hosted service is running.
/// </summary>
/// <param name="attemptLoggingService"></param>
internal class HostedServicesCheck(LoginAttemptLoggingService attemptLoggingService) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Task? task = attemptLoggingService.ExecuteTask;

        HealthCheckResult result = task switch
        {
            { IsCompleted: false } => HealthCheckResult.Healthy("Login attempt logging service is running or awaiting its runtime."),
            { IsFaulted: true } => HealthCheckResult.Unhealthy(task.Exception?.InnerException?.Message ?? "An unknown exception occurred during logging LoginAttempts.", task.Exception!.InnerException),
            { IsCanceled: true } => HealthCheckResult.Unhealthy("Login attempt logging service was cancelled."),
            { IsCompleted: true } => HealthCheckResult.Unhealthy("Login attempt logging service end. Unable to log login attempts anymore."),
            _ => HealthCheckResult.Degraded("Unable to determine health status of the login attempt logging service.")
        };
        return Task.FromResult(result);
    }
}
