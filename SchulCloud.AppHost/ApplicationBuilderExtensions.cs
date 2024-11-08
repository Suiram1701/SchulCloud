using Aspire.Hosting.MailDev;
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

        IResourceBuilder<ParameterResource> username = builder.AddParameter($"{name}-Username", secret: true);
        IResourceBuilder<ParameterResource> password = builder.AddParameter($"{name}-Password", secret: true);

        return builder
            .AddPostgres(name, userName: username, password: password)
            .WithDataVolume()
            .WithPgAdmin();
    }

    public static IResourceBuilder<MailDevResource> AddMailDev(this IDistributedApplicationBuilder builder, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        IResourceBuilder<ParameterResource> username = builder.AddParameter($"{name}-Username", secret: true);
        IResourceBuilder<ParameterResource> password = builder.AddParameter($"{name}-Password", secret: true);

        return builder.AddMailDev(name, username: username, password: password);
    }
}
