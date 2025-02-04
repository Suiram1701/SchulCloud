using Aspire.Hosting.MailDev;
using Aspire.Hosting.MinIO;
using SchulCloud.AppHost.Extensions;

namespace SchulCloud.AppHost.Extensions;

internal static class ApplicationBuilderExtensions
{
    public static IResourceBuilder<PostgresServerResource> AddPostgresServer(this IDistributedApplicationBuilder builder, string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        IResourceBuilder<ParameterResource> username = builder.AddParameterFromConfiguration($"{name}-Username", $"{name}:Username", secret: true);
        IResourceBuilder<ParameterResource> password = builder.AddParameterFromConfiguration($"{name}-Password", $"{name}:Password", secret: true);

        return builder.AddMailDev(name, username: username, password: password);
    }

    public static IResourceBuilder<MinIOServerResource> AddMinIO(this IDistributedApplicationBuilder builder, string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        IResourceBuilder<ParameterResource> username = builder.AddParameterFromConfiguration($"{name}-Username", $"{name}:Username", secret: true);
        IResourceBuilder<ParameterResource> password = builder.AddParameterFromConfiguration($"{name}-Password", $"{name}:Password", secret: true);


        return builder
            .AddMinIO(name, username: username, password: password)
            .WithConsole()
            .WithDataVolume();
    }
}
