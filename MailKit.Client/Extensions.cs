using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailKit.Client;

public static class Extensions
{
    /// <summary>
    /// Registers 'Scoped' <see cref="MailKitClientFactory" /> for creating connected <see cref="SmtpClient"/> instance for sending emails.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    public static void AddMailKitClient(this IHostApplicationBuilder builder, string connectionName, Action<MailKitClientSettings>? configureSettings = null) =>
        AddMailKitClient(builder, MailKitClientSettings._defaultConfigSectionName, configureSettings, connectionName, serviceKey: null);

    /// <summary>
    /// Registers 'Scoped' <see cref="MailKitClientFactory" /> for creating connected <see cref="SmtpClient"/> instance for sending emails.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    public static void AddKeyedMailKitClient(this IHostApplicationBuilder builder, string name, Action<MailKitClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        AddMailKitClient(builder, $"{MailKitClientSettings._defaultConfigSectionName}:{name}", configureSettings, connectionName: name, serviceKey: name);
    }

    private static void AddMailKitClient(this IHostApplicationBuilder builder, string configurationSectionName, Action<MailKitClientSettings>? configureSettings, string connectionName, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new MailKitClientSettings();
        builder.Configuration
               .GetSection(configurationSectionName)
               .Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ParseConnectionString(connectionString);
        }
        configureSettings?.Invoke(settings);

        if (serviceKey is null)
        {
            builder.Services.AddScoped(provider => new MailKitClientFactory(provider.GetRequiredService<ILogger<MailKitClientFactory>>(), settings));
        }
        else
        {
            builder.Services.AddKeyedScoped(serviceKey, (provider, _) => new MailKitClientFactory(provider.GetRequiredService<ILogger<MailKitClientFactory>>(), settings));
        }

        if (settings.DisableHealthChecks is false)
        {
            builder.Services.AddHealthChecks()
                .AddCheck<MailKitHealthCheck>(serviceKey is null ? "MailKit" : $"MailKit_{connectionName}", default, []);
        }

        if (settings.DisableTracing is false)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(traceBuilder => traceBuilder.AddSource(Telemetry.SmtpClient.ActivitySourceName));
        }

        if (settings.DisableMetrics is false)
        {
            Telemetry.SmtpClient.Configure();

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metricsBuilder => metricsBuilder.AddMeter(Telemetry.SmtpClient.MeterName));
        }
    }
}
