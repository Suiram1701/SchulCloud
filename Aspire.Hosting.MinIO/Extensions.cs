using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.MinIO;

public static class Extensions
{
    /// <summary>
    /// Adds a MinIO container to the distributed application providing an S3 compatible storage.
    /// </summary>
    /// <param name="builder">The distributed app builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="httpPort">The exposed http port of the S3 API.</param>
    /// <param name="username">The admin username of MinIO.</param>
    /// <param name="password">The admin password of MinIO.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<MinIOResource> AddMinIO(
        this IDistributedApplicationBuilder builder,
        string name,
        int? httpPort = null,
        IResourceBuilder<ParameterResource>? username = null,
        IResourceBuilder<ParameterResource>? password = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ParameterResource usernameResource = username?.Resource ?? ParameterResourceBuilderExtensions.CreateGeneratedParameter(builder, $"{name}-Username", secret: false, new());
        ParameterResource passwordResource = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-Password");

        MinIOResource minIO = new(name, usernameResource, passwordResource);
        return builder.AddResource(minIO)
            .WithImage("minio/minio", tag: "RELEASE.2025-01-20T14-49-07Z")
            .WithImageRegistry("docker.io")
            .WithArgs("server", "/var/lib/minio/data", "--address", ":9000")
            .WithHttpEndpoint(port: httpPort, targetPort: 9000)
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["MINIO_ROOT_USER"] = minIO.UsernameParameter;
                context.EnvironmentVariables["MINIO_ROOT_PASSWORD"] = minIO.PasswordParameter;
            });
    }

    /// <summary>
    /// Adds the web ui interface to a MinIO container.
    /// </summary>
    /// <param name="builder">The MinIO container to add this to.</param>
    /// <param name="port">The exposed http port of the web interface</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<MinIOResource> WithConsole(this IResourceBuilder<MinIOResource> builder, int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .WithArgs("--console-address", ":9001")
            .WithHttpEndpoint(name: "console", port: port, targetPort: 9001);
    }

    /// <summary>
    /// Adds a persistent volume to the MinIO container.
    /// </summary>
    /// <param name="builder">The MinIO container to add this to.</param>
    /// <param name="name">The name of the volume.</param>
    /// <param name="isReadOnly">Specified whether the volume if read only.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<MinIOResource> WithDataVolume(this IResourceBuilder<MinIOResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), "/var/lib/minio/data", isReadOnly);
    }
}
