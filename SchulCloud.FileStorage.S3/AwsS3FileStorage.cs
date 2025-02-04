using Amazon.S3;
using Amazon.S3.Model;
using AwsS3.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchulCloud.FileStorage.Abstractions;
using System.Net.Mime;

namespace SchulCloud.FileStorage.S3;

/// <summary>
/// An implementation file storage stores that will use an AWS S3 bucket.
/// </summary>
/// <typeparam name="TUser">The type of user.</typeparam>
/// <param name="logger">A logger this instance will use.</param>
/// <param name="configuration">
/// A configuration that will be used if the bucket name is not set by <c><paramref name="clientFactory"/>.Settings</c>. 
/// The bucket name will be retrieved from the configuration key <c>S3:BucketName</c>.
/// </param>
/// <param name="clientFactory">The client factory to get the AWS S3 client.</param>
/// <param name="userManager">A user manager that will be used to get information from user instances.</param>
public partial class AwsS3FileStorage<TUser>(
    ILogger<AwsS3FileStorage<TUser>> logger,
    IConfiguration configuration,
    AwsS3ClientFactory clientFactory,
    UserManager<TUser> userManager) : IProfileImageStore<TUser>
    where TUser : class
{
    /// <summary>
    /// The name of the bucket this storage will use.
    /// </summary>
    public string BucketName => clientFactory.Settings.BucketName ?? GetBucketNameFromConfig();

    private readonly IAmazonS3 _client = clientFactory.GetS3Client();

    public async Task<Stream?> GetImageAsync(TUser user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await userManager.GetUserIdAsync(user);
        string objectKey = GetProfileImageObjectKey(userId);
        try
        {
            GetObjectResponse response = await _client.GetObjectAsync(BucketName, objectKey, ct).ConfigureAwait(false);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            LogAwsS3Exception(logger, ex, BucketName, objectKey);
            throw;
        }
    }

    public async Task UpdateImageAsync(TUser user, Stream image, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(image);
        ct.ThrowIfCancellationRequested();

        string userId = await userManager.GetUserIdAsync(user);
        string objectKey = GetProfileImageObjectKey(userId);
        try
        {
            _ = await _client.PutObjectAsync(new()
            {
                Key = objectKey,
                BucketName = BucketName,
                InputStream = image,
                ContentType = MediaTypeNames.Image.Png
            }, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            LogAwsS3Exception(logger, ex, BucketName, objectKey);
            throw;
        }
    }

    public async Task RemoveImageAsync(TUser user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await userManager.GetUserIdAsync(user);
        string objectKey = GetProfileImageObjectKey(userId);
        try
        {
            _ = await _client.DeleteObjectAsync(BucketName, objectKey, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            LogAwsS3Exception(logger, ex, BucketName, objectKey);
            throw;
        }
    }

    private string GetBucketNameFromConfig() => configuration["S3:BucketName"] ?? throw new ArgumentNullException("Bucket configuration through 'S3:BucketName' is missing.");

    private static string GetProfileImageObjectKey(string userId) => $"profile-images/{userId}.png";

    [LoggerMessage(LogLevel.Error, "An unexpected error occurred while accessing an AWS S3 object. BucketName: {bucket}; ObjectKey: {object}")]
    private static partial void LogAwsS3Exception(ILogger logger, AmazonS3Exception ex, string bucket, string @object);
}