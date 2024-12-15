using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.AppHost.Extensions;

internal static class ResourceBuilderExtensions
{
    /// <summary>
    /// Adds the default health check endpoint offered by ServiceDefaults to the resource.
    /// </summary>
    /// <remarks>
    /// The endpoints are only added when the application is is development environment.
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder pipeline.</returns>
    public static IResourceBuilder<TResource> WithDefaultHealthChecks<TResource>(this IResourceBuilder<TResource> builder)
        where TResource : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            builder
                .WithHttpHealthCheck(path: "/alive")
                .WithHttpHealthCheck(path: "/health");
        }
        return builder;
    }

    /// <summary>
    /// Adds commands to a resource that are used by every service
    /// </summary>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder to use.</param>
    /// <returns>The builder pipeline.</returns>
    public static IResourceBuilder<TResource> WithDefaultCommands<TResource>(this IResourceBuilder<TResource> builder)
        where TResource : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithHttpCommand(
            name: "clear-cache",
            displayName: "Clear cache",
            path: "/commands/clear-cache",
            description: "Clears the in memory cache of this application.",
            iconName: "DocumentDismiss");
    }

    /// <summary>
    /// Adds commands to a resource that are implemented by a database manager.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource. Should be <see cref="ProjectResource"/>.</typeparam>
    /// <param name="builder">The resource builder to use.</param>
    /// <returns>The builder pipeline.</returns>
    public static IResourceBuilder<TResource> WithDbManagerCommands<TResource>(this IResourceBuilder<TResource> builder)
        where TResource : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithHttpCommand(
            name: "initialize-db",
            displayName: "Initialize database",
            path: "/commands/initialize-db",
            description: "Tries to initialize the database with the recent migrations and the default user if not present.",
            confirmMessage: "WARNING: This action is irreversible and can cause loss of data depending on the migration and can create a new user with admin privileges.",
            iconName: "Database");
        builder.WithHttpCommand(
            name: "drop-db",
            displayName: "Drop database",
            path: "/commands/drop-db",
            description: "Drops all databases that this instance manages entirely. This will also pause the cleaner.",
            confirmMessage: "WARNING: This will remove every databases completely. This action is irreversible and will cause the loss of every users data. Are you sure you want to continue?",
            iconName: "Delete");

        builder.WithHttpCommand(
            name: "cleanup",
            displayName: "Cleanup",
            path: "/commands/cleanup",
            description: "Starts an manually cleanup cycle. Normal scheduled cleanup cycles are not influenced by this.",
            iconName: "Broom");
        builder.WithHttpCommand(
            name: "cleanup-pause",
            displayName: "Pause cleaner",
            path: "/commands/cleanup-pause",
            description: "Pauses the automated cleanup cycles. If already paused no action is taken.",
            iconName: "ClockPause");
        builder.WithHttpCommand(
            name: "cleanup-resume",
            displayName: "Resume cleaner",
            path: "/commands/cleanup-resume",
            description: "Resumes the cleanup cycles if paused. If not paused no action is taken.",
            iconName: "Clock");

        return builder;
    }

    /// <summary>
    /// Adds a command that performs a http request to an endpoint of this app.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource to add the command to.</typeparam>
    /// <param name="builder">The resource builder to use.</param>
    /// <param name="name">The internal name of the command.</param>
    /// <param name="displayName">The in the ui displayed name of the command.</param>
    /// <param name="path">The request uri to use.</param>
    /// <param name="endpointName">The name of the endpoint to use. By default is <c>http</c> used.</param>
    /// <param name="method">The http request method to use. By default if <see cref="HttpMethod.Get"/> used.</param>
    /// <param name="updateState">A callback that is triggered to check whether the current state of the app allows it to perform this command.</param>
    /// <param name="description">A description that is shown in the ui.</param>
    /// <param name="confirmMessage">A message to show before the command will be performed.</param>
    /// <param name="iconName">A name of a FluentUI icon to show for the command button.</param>
    /// <param name="iconVariant">The variant of the icon to use.</param>
    /// <returns>The resource builder pipeline.</returns>
    public static IResourceBuilder<TResource> WithHttpCommand<TResource>(
        this IResourceBuilder<TResource> builder,
        string name,
        string displayName,
        string path,
        string? endpointName = null,
        HttpMethod? method = null,
        Func<UpdateCommandStateContext, ResourceCommandState>? updateState = null,
        string? description = null,
        string? confirmMessage = null,
        string? iconName = default,
        IconVariant? iconVariant = null)
        where TResource : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);

        method ??= HttpMethod.Get;
        endpointName ??= "http";

        EndpointReference endpoint = builder.Resource.GetEndpoints()
            .FirstOrDefault(endpoint => endpoint.EndpointName == endpointName)
            ?? throw new DistributedApplicationException($"Could not create HTTP command for resource '{builder.Resource.Name}' as no endpoint named '{endpointName}' was found.");

        return builder.WithCommand(
            name: $"http-{name}",
            displayName: displayName,
            executeCommand: async context =>
            {
                if (!endpoint.IsAllocated)
                {
                    return new ExecuteCommandResult { Success = false, ErrorMessage = "Endpoints are not yet allocated." };
                }

                Uri uri = new UriBuilder(endpoint.Url) { Path = path }.Uri;
                HttpClient httpClient = context.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
                try
                {
                    using HttpRequestMessage request = new(method, uri);
                    using HttpResponseMessage response = await httpClient.SendAsync(request, context.CancellationToken);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    return new ExecuteCommandResult { Success = false, ErrorMessage = ex.Message };
                }
                return new ExecuteCommandResult { Success = true };
            },
            updateState: updateState,
            displayDescription: description,
            confirmationMessage: confirmMessage,
            iconName: iconName,
            iconVariant: iconVariant);
    }
}
