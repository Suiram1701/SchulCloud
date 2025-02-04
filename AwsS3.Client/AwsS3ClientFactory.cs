using Amazon.S3;
using Microsoft.Extensions.Logging;

namespace AwsS3.Client;

/// <summary>
/// A factory for <see cref="IAmazonS3"/> clients.
/// </summary>
/// <param name="logger">A logger to use.</param>
/// <param name="settings">Settings to use for client construction.</param>
public sealed class AwsS3ClientFactory(ILogger<AwsS3ClientFactory> logger, AwsS3ClientSettings settings) : IDisposable
{
    /// <summary>
    /// The settings this factory uses.
    /// </summary>
    public AwsS3ClientSettings Settings => settings;

    private AmazonS3Client? _client;

    /// <summary>
    /// Returns an existing client if available or creates a new one if not.
    /// </summary>
    /// <returns>An <see cref="IAmazonS3"/> client.</returns>
    public IAmazonS3 GetS3Client()
    {
        if (_client is null)
        {
            try
            {
                _client = new AmazonS3Client(settings.Credentials, settings.AwsS3Config);
                logger.LogDebug("New AWS S3 client created");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating an AWS S3 client.");
                throw;
            }
        }

        return _client;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
