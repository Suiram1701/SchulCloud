using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.MinIO;

/// <summary>
/// A resource that represents an MinIO bucket. This is a child resource of <see cref="MinIOServerResource"/>.
/// </summary>
/// <param name="name">The name of this resource.</param>
/// <param name="bucketName">The name of the bucket.</param>
/// <param name="minIOParentResource">The MinIO parent server.</param>
public class MinIOBucketResource(string name, string bucketName, MinIOServerResource minIOParentResource) : Resource(name), IResourceWithParent<MinIOServerResource>, IResourceWithConnectionString
{
    public MinIOServerResource Parent => ThrowIfNull(minIOParentResource);

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{Parent};Bucket={BucketName}");

    /// <summary>
    /// The name of this bucket.
    /// </summary>
    public string BucketName { get; } = ThrowIfNull(bucketName);

    private static T ThrowIfNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null) => argument ?? throw new ArgumentNullException(paramName);
}