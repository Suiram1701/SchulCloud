using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SchulCloud.ServiceDefaults.Authentication;
using SchulCloud.ServiceDefaults.Metrics;
using SchulCloud.ServiceDefaults.Options;
using SchulCloud.ServiceDefaults.Services;

namespace SchulCloud.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
        });

        builder.Services.Configure<ServiceOptions>(options =>
        {
            options.BasePath = builder.Configuration["BasePath"] ?? "/";
        });

        builder.ConfigureCommands();
        builder.ConfigureMemoryCacheMetrics();

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(MemoryCacheMetrics.Name);
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static IHostApplicationBuilder ConfigureCommands(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAuthentication()
            .AddScheme<StaticKeySchemeOptions, StaticKeyScheme>("command-api", configure =>
            {
                configure.Key = builder.Configuration.GetValue<string>("Commands:ApiKey");
            });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("command-api", policy =>
            {
                policy.AuthenticationSchemes = ["command-api"];
                policy.RequireAuthenticatedUser();
            });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("command-fixed", configure =>
            {
                if (builder.Configuration.GetSection("Commands:RateLimiter").Exists())
                {
                    builder.Configuration.Bind("Commands:RateLimiter", configure);
                }
                else
                {
                    configure.Window = TimeSpan.MinValue;
                    configure.PermitLimit = int.MaxValue;
                }
            });
        });

        builder.Services.AddProblemDetails();
        return builder;
    }

    public static IHostApplicationBuilder ConfigureMemoryCacheMetrics(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<MemoryCacheOptions>(options => options.TrackStatistics = true);

        builder.Services.AddSingleton<MemoryCacheMetrics>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<MemoryCacheMetrics>());

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app, Action<IEndpointRouteBuilder>? commandsBuilder = null )
    {
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health", new()
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).DisableHttpMetrics();

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
                Predicate = r => r.Tags.Contains("live")
            }).DisableHttpMetrics();

            app.MapCommands(commandsBuilder);
        }

        return app;
    }

    /// <summary>
    /// Adds default commands for the aspire ui.
    /// </summary>
    /// <param name="app">The application builder to use.</param>
    /// <returns></returns>
    public static WebApplication MapCommands(this WebApplication app, Action<IEndpointRouteBuilder>? commandsBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapWhen(context => context.Request.Path.StartsWithSegments("/commands"), subApp =>
        {
            subApp.UseRouting();

            subApp.UseAuthentication();
            subApp.UseAuthorization();

            subApp.UseRateLimiter();

            subApp.UseEndpoints(routeBuilder =>
            {
                RouteGroupBuilder groupBuilder = routeBuilder.MapGroup("/commands")
                    .RequireAuthorization("command-api")
                    .RequireRateLimiting("command-fixed");
                groupBuilder.MapGet("/clear-cache", async context =>
                {
                    if (context.RequestServices.GetService<IMemoryCache>() is MemoryCache cache)
                    {
                        cache.Clear();
                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                    }
                    else
                    {
                        IProblemDetailsService problemService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
                        await problemService.WriteAsync(new()
                        {
                            HttpContext = context,
                            ProblemDetails = new()
                            {
                                Title = "Unable to clear cache",
                                Status = StatusCodes.Status501NotImplemented,
                                Detail = "The server does not implement a clearable cache."
                            }
                        }).ConfigureAwait(false);
                    }
                });
                commandsBuilder?.Invoke(groupBuilder);
            });
        });

        return app;
    }

    /// <summary>
    /// Adds a postgres database from a aspire environment to the services.
    /// </summary>
    /// <typeparam name="TContext">The type of the database context.</typeparam>
    /// <param name="builder">The builder of the app.</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="pooledService">Indicates whether the context have to be added as a pooled service. By default <c>true</c>.</param>
    public static void AddAspirePostgresDb<TContext>(this IHostApplicationBuilder builder, string resourceName, bool pooledService = true)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        if (pooledService)
        {
            builder.AddNpgsqlDbContext<TContext>(resourceName, configureDbContextOptions: options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });
        }
        else
        {
            builder.Services.AddDbContext<TContext>(options =>
            {
                string connectionString = builder.Configuration.GetConnectionString(resourceName)
                    ?? throw new InvalidOperationException($"A connection string for the resource '{resourceName} was expected.'");

                options.UseNpgsql(connectionString);

                if (builder.Environment.IsDevelopment())
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });
            builder.EnrichNpgsqlDbContext<TContext>();
        }
    }

    public static IServiceCollection AddDataManager<TManager>(this IServiceCollection services)
        where TManager : class, IDataManager
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddScoped<IDataManager, TManager>();
    }
}
