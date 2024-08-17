using System.Data.Common;
using System.Net;

namespace MailKit.Client;

/// <summary>
/// Provides the client configuration settings for MailKit.
/// </summary>
public sealed class MailKitClientSettings
{
    internal const string _defaultConfigSectionName = "MailKit:Client";

    /// <summary>
    /// The endpoint of the mail server.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Optional credentials that can be used when the mail server requires authentication.
    /// </summary>
    public NetworkCredential? Credentials { get; set; }

    /// <summary>
    /// An optional interval at which the server is pinged to keep the connection alive.
    /// </summary>
    public TimeSpan? KeepAliveIntervall { get; set; }

    /// <summary>
    /// Indicates whether health checks should be disabled.
    /// </summary>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Indicates whether tracing via OpenTelemetry should be disabled.
    /// </summary>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Indicates whether metrics via OpenTelemetry should be disabled.
    /// </summary>
    public bool DisableMetrics { get; set; }

    internal void ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string is missing. " +
                $"It should be provided in 'ConnectionStrings:<connectionName> or '{_defaultConfigSectionName}:Endpoint' configuration section.'");
        }

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out Uri? endpoint))
        {
            Endpoint = endpoint;
        }
        else
        {
            DbConnectionStringBuilder builder = new()
            {
                ConnectionString = connectionString
            };

            if (builder.TryGetValue("Endpoint", out object? endpointObj))
            {
                if (!Uri.TryCreate(endpointObj.ToString(), UriKind.Absolute, out endpoint))
                {
                    throw new InvalidOperationException(
                        $"The Endpoint in the connection string is missing or isn't a valid Uri. " +
                        $"It should be provided in 'ConnectionStrings:<connectionName> or '{_defaultConfigSectionName}:Endpoint' configuration section.'");
                }

                Endpoint = endpoint;
            }

            if (builder.TryGetValue("Username", out object? username) && builder.TryGetValue("password", out object? password))
            {
                Credentials = new(username.ToString(), password.ToString());
            }
        }
    }
}
