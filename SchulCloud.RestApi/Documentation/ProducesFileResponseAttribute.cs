using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace SchulCloud.RestApi.Documentation;

/// <summary>
/// Provides automated swagger documentation for an action returning a file.
/// </summary>
/// <remarks>
/// This will always have the status code 200.
/// </remarks>
/// <param name="contentType">The content of the action will return.</param>
/// <param name="supportsRangeProcessing">Enables generation of the range docs.</param>
/// <param name="additionalContentTypes">Additional content types of this response.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ProducesFileResponseAttribute(string contentType, bool supportsRangeProcessing = false, params string[] additionalContentTypes)
    : ProducesResponseTypeAttribute(typeof(FileStreamResult), StatusCodes.Status200OK, contentType, additionalContentTypes)
{
    /// <summary>
    /// Indicates whether this action will support range processing.
    /// </summary>
    public bool SupportsRangeProcessing { get; } = supportsRangeProcessing;

    /// <summary>
    /// Get the possible content types this action may return.
    /// </summary>
    public MediaTypeCollection ContentTypes => _contentTypes ??= GetContentTypes();
    private MediaTypeCollection? _contentTypes;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="contentType">The content type this action returns.</param>
    /// <param name="supportsRangeProcessing">Enabled generation of the range docs.</param>
    public ProducesFileResponseAttribute(string contentType = Application.Octet, bool supportsRangeProcessing = false)
        : this(contentType, supportsRangeProcessing, additionalContentTypes: [])
    {
    }

    private MediaTypeCollection GetContentTypes()
    {
        MediaTypeCollection types = [];
        ((IApiResponseMetadataProvider)this).SetContentTypes(types);

        return types;
    }
}
