using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace SchulCloud.RestApi.Extensions;

/// <summary>
/// Extensions for <see cref="WebApplication"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds an exception handler to the middleware pipeline that can be used for production.
    /// </summary>
    /// <param name="application">The middleware pipeline.</param>
    /// <returns>The pipeline</returns>
    public static IApplicationBuilder UseProductionExceptionHandler(this IApplicationBuilder application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.UseExceptionHandler(subApp =>
        {
            subApp.Use(async (context, next) =>
            {
                IProblemDetailsService problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await problemDetailsService.WriteAsync(new()
                {
                    HttpContext = context,
                    ProblemDetails = new()
                    {
                        Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError),
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = "An error occurred while processing the request."
                    }
                }).ConfigureAwait(false);

                await next(context).ConfigureAwait(false);
            });
        });
    }

    /// <summary>
    /// Adds a the header 'X-Trace-Id' to every response which contains the trace id of the request.
    /// </summary>
    /// <param name="application">The application pipeline.</param>
    /// <returns>The pipeline</returns>
    public static IApplicationBuilder UseTraceHeader(this IApplicationBuilder application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.Use((context, next) =>
        {
            context.Response.Headers.TryAdd("X-Trace-Id", context.TraceIdentifier);
            return next(context);
        });
    }
}
