using Microsoft.Extensions.Options;
using SchulCloud.ServiceDefaults.Options;

namespace SchulCloud.RestApi.Extensions;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a customized variant of a problem details response that provides 'instance' and 'requestId'.
    /// </summary>
    /// <param name="services">The collection to add this to.</param>
    /// <returns>THe service collection.</returns>
    public static IServiceCollection AddCustomizedProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                ServiceOptions serviceOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<ServiceOptions>>().Value;

                HttpRequest request = context.HttpContext.Request;
                context.ProblemDetails.Instance = $"{request.Method} {serviceOptions.GetPathWithBase(request.Path)}";

                context.ProblemDetails.Extensions ??= new Dictionary<string, object?>();
                context.ProblemDetails.Extensions.Add("requestId", context.HttpContext.TraceIdentifier);
            };
        });
    }
}
