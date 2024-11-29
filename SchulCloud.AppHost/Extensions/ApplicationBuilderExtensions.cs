using Aspire.Hosting.MailDev;
using Microsoft.Extensions.Hosting;
using SchulCloud.AppHost.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.AppHost.Extensions;

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
}
