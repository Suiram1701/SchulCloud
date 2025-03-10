using System.Runtime.CompilerServices;

namespace SchulCloud.FileStorage;

/// <summary>
/// The result of retrieving a file from the store.
/// </summary>
public sealed class FileResultInfo(Stream fileStream, string? contentType = null, string? eTag = null, DateTimeOffset? lastModified = null)
    : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The binary content of the file.
    /// </summary>
    public Stream FileStream { get; } = ThrowIfNull(fileStream);

    /// <summary>
    /// The MIME content type of the file if available.
    /// </summary>
    public string? ContentType { get; } = contentType;

    /// <summary>
    /// The entity tag (ETag) of the content if available.
    /// </summary>
    public string? ETag { get; } = eTag;

    /// <summary>
    /// The date time where the file were modified last time if available.
    /// </summary>
    public DateTimeOffset? LastModified { get; } = lastModified;

    public void Dispose() => FileStream.Dispose();

    public async ValueTask DisposeAsync() => await FileStream.DisposeAsync().ConfigureAwait(false);

    private static T ThrowIfNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }
}
