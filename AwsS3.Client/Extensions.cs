using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AwsS3.Client;

public static class Extensions
{
    /// <summary>
    /// Adds a <see cref="AwsS3ClientFactory"/> to the services providing a client constructed from the MinIO connection provided.
    /// </summary>
    /// <param name="builder">The app builder to use.</param>
    /// <param name="name">The name of the MinIO connection.</param>
    /// <param name="configureSettings">An action to configure further settings.</param>
    public static void AddMinIOAwsClient(this IHostApplicationBuilder builder, string name, Action<AwsS3ClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        builder.AddMinIOAwsClient(name, AwsS3ClientSettings._defaultConfigSectionName, configureSettings, serviceKey: null);
    }

    /// <summary>
    /// Adds a keyed <see cref="AwsS3ClientFactory"/> to the services providing a client constructed from the MinIO connection provided.
    /// </summary>
    /// <param name="builder">The app builder to use.</param>
    /// <param name="name">The name of the MinIO connection and the key of the service.</param>
    /// <param name="configureSettings">Am action to configure further settings.</param>
    public static void AddKeyedMinIOAwsClient(this IHostApplicationBuilder builder, string name, Action<AwsS3ClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        builder.AddMinIOAwsClient(name, $"{AwsS3ClientSettings._defaultConfigSectionName}:{name}", configureSettings, serviceKey: name);
    }

    private static void AddMinIOAwsClient(this IHostApplicationBuilder builder, string connectionName, string configurationSection, Action<AwsS3ClientSettings>? configureSettings, object? serviceKey)
    {
        AwsS3ClientSettings settings = new();
        builder.Configuration
            .GetSection(configurationSection)
            .Bind(settings);
        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ParseMinIOConnectionString(connectionString);
        }
        configureSettings?.Invoke(settings);

        if (serviceKey is null)
        {
            builder.Services.AddScoped(provider => new AwsS3ClientFactory(provider.GetRequiredService<ILogger<AwsS3ClientFactory>>(), settings));
        }
        else
        {
            builder.Services.AddKeyedScoped(serviceKey, (provider, _) => new AwsS3ClientFactory(provider.GetRequiredService<ILogger<AwsS3ClientFactory>>(), settings));
        }

        if (!settings.DisableHealthChecks)
        {
            builder.Services.AddHealthChecks()
                .AddCheck<AwsS3HealthCheck>(serviceKey is null ? "AwsS3" : $"AwsS3_{connectionName}");
        }
    }
}
