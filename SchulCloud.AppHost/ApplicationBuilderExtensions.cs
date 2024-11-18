using Aspire.Hosting.MailDev;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.AppHost;

internal static class ApplicationBuilderExtensions
{
    public static IResourceBuilder<PostgresServerResource> AddPostgresServer(this IDistributedApplicationBuilder builder, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        IResourceBuilder<ParameterResource> username = builder.AddParameterFromConfiguration($"{name}-Username", $"{name}:Username", secret: true);
        IResourceBuilder<ParameterResource> password = builder.AddParameterFromConfiguration($"{name}-Password", $"{name}:Password", secret: true);

        return builder
            .AddPostgres(name, userName: username, password: password)
            .WithDataVolume()
            .WithPgAdmin();
    }

    public static IResourceBuilder<MailDevResource> AddMailDev(this IDistributedApplicationBuilder builder, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        IResourceBuilder<ParameterResource> username = builder.AddParameterFromConfiguration($"{name}-Username", $"{name}:Username", secret: true);
        IResourceBuilder<ParameterResource> password = builder.AddParameterFromConfiguration($"{name}-Password", $"{name}:Password", secret: true);

        return builder.AddMailDev(name, username: username, password: password);
    }

    /// <summary>
    /// Adds the default health check endpoint offered by ServiceDefaults to the resource.
    /// </summary>
    /// <remarks>
    /// The endpoints are only added when the application is is development environment.
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder pipeline.</returns>
    public static IResourceBuilder<ProjectResource> WithDefaultHealthChecks(this IResourceBuilder<ProjectResource> builder)
    {
        if (builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            builder
                .WithHttpHealthCheck(path: "/alive")
                .WithHttpHealthCheck(path: "/health");
        }

        return builder;
    }
}
