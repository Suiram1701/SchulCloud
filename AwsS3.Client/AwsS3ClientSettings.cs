using Amazon.Runtime;
using Amazon.S3;
using System.Data.Common;

namespace AwsS3.Client;

/// <summary>
/// A class providing settings for <see cref="AwsS3ClientFactory"/>.
/// </summary>
public class AwsS3ClientSettings
{
    internal const string _defaultConfigSectionName = "AwsS3:Client";

    /// <summary>
    /// The credentials to use for authentication. <see cref="BasicAWSCredentials"/> is used instead of an abstraction to make it configurable through json config.
    /// </summary>
    public BasicAWSCredentials Credentials { get; set; } = new(string.Empty, string.Empty);

    /// <summary>
    /// The configuration to use.
    /// </summary>
    public AmazonS3Config AwsS3Config { get; set; } = new();

    /// <summary>
    /// The name of the bucket to use.
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// Indicates whether health checks should be disabled.
    /// </summary>
    public bool DisableHealthChecks { get; set; }

    internal void ParseMinIOConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"The connection string is missing. It should be provided in 'ConnectionString:<connectionName>'");
        }

        AwsS3Config ??= new();
        AwsS3Config.ForcePathStyle = true;
        AwsS3Config.SignatureVersion = "4";

        DbConnectionStringBuilder builder = new()
        {
            ConnectionString = connectionString
        };
        if (builder.TryGetValue("Endpoint", out object? endpointObj))
        {
            if (Uri.TryCreate(endpointObj.ToString(), UriKind.Absolute, out Uri? endpoint))
            {
                AwsS3Config.ServiceURL = endpoint!.ToString();
            }
            else
            {
                throw new InvalidOperationException("Unable to extract the service endpoint from the connection string.");
            }
        }
        if (builder.TryGetValue("Username", out object? username) && builder.TryGetValue("Password", out object? password))
        {
            Credentials = new(username.ToString(), password.ToString());
        }
        if (builder.TryGetValue("Bucket", out object? bucketName))
        {
            BucketName = bucketName.ToString();
        }
    }
}
