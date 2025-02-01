using Amazon.S3;
using Amazon.S3.Model;
using AwsS3.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchulCloud.FileStorage.Abstractions;
using System.Net.Mime;

namespace SchulCloud.FileStorage.S3;

public partial class AwsS3FileStorage<TUser>(
    ILogger<AwsS3FileStorage<TUser>> logger,
    IConfiguration configuration,
    AwsS3ClientFactory clientFactory,
    UserManager<TUser> userManager) : IProfileImageStore<TUser>
    where TUser : class
{
    private readonly string _bucketName = configuration["S3:BucketName"]
        ?? throw new ArgumentNullException("Bucket configuration through 'S3:BucketName' is missing.");
    private readonly IAmazonS3 _client = clientFactory.GetS3Client();

    public async Task<Stream?> GetImageAsync(TUser user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await userManager.GetUserIdAsync(user);
        string objectKey = GetProfileImageObjectKey(userId);
        try
        {
            GetObjectResponse response = await _client.GetObjectAsync(_bucketName, objectKey, ct).ConfigureAwait(false);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            LogAwsS3Exception(logger, ex, _bucketName, objectKey);
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
                BucketName = _bucketName,
                InputStream = image,
                ContentType = MediaTypeNames.Image.Png
            }, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            LogAwsS3Exception(logger, ex, _bucketName, objectKey);
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
            _ = await _client.DeleteObjectAsync(_bucketName, objectKey, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            LogAwsS3Exception(logger, ex, _bucketName, objectKey);
            throw;
        }
    }

    private static string GetProfileImageObjectKey(string userId) => $"profile-images/{userId}.png";

    [LoggerMessage(LogLevel.Error, "An unexpected error occurred while accessing an AWS S3 object. BucketName: {bucket}; ObjectKey: {object}")]
    private static partial void LogAwsS3Exception(ILogger logger, AmazonS3Exception ex, string bucket, string @object);
}