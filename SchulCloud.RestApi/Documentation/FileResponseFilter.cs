using System.Text.Json.Nodes;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SchulCloud.RestApi.Documentation;

internal sealed class FileResponseFilter : AttributeFilterBase<ProducesFileResponseAttribute>
{
    private static readonly JsonValue _dateTimeExample = JsonValue.Create(new DateTimeOffset(new DateTime(2020, 01, 01)));

    protected override void Apply(OpenApiOperation operation, OperationFilterContext context, ProducesFileResponseAttribute[] attributes)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();
        operation.Responses ??= new OpenApiResponses();
        
        ProducesFileResponseAttribute attribute = attributes[0];     // Only one is allowed.
        if (attribute.SupportsRangeProcessing)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = HeaderNames.Range,
                In = ParameterLocation.Header,
                Description = "Indicates the part of the resource that should be returned.",
                Example = JsonValue.Create("bytes=0-499"),
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Pattern = @"^bytes=(\d*)-(\d*)$"
                }
            });
        }

        // Conditional headers
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderNames.IfMatch,
            In = ParameterLocation.Header,
            Description = "The resource will only be returned if the ETag matches the provided one."
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderNames.IfNoneMatch,
            In = ParameterLocation.Header,
            Description = "The resource will only be returned if the ETag doesn't matches the provided one."
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderNames.IfModifiedSince,
            In = ParameterLocation.Header,
            Description = "Ensures the resource is only returned if it was modified since the specific date time.",
            Example = _dateTimeExample
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderNames.IfUnmodifiedSince,
            In = ParameterLocation.Header,
            Description = "Ensures the resource is only returned if it wasn't modified since the specific date time.",
            Example = _dateTimeExample
        });

        IOpenApiResponse ok = operation.Responses[StatusCodes.Status200OK.ToString()];
        foreach (string type in attribute.ContentTypes)
        {
            ok.Content?.TryAdd(type, new OpenApiMediaType());
            ok.Content?[type].Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Pattern = "binary"
            };
        }
        EnrichResponseHeaders(operation.Responses[StatusCodes.Status200OK.ToString()], attribute.SupportsRangeProcessing);

        if (attribute.SupportsRangeProcessing)
        {
            OpenApiResponse partialContent = new()
            {
                Description = "Returned when only the requested part of the resource were returned.",
                Content = attribute.ContentTypes.ToDictionary(
                    keySelector: value => value,
                    elementSelector: _ => new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Pattern = "binary"
                        }
                        
                    })
            };
            partialContent.Headers?.Add(HeaderNames.ContentRange, new OpenApiHeader
            {
                Description = "Indicates where the content of a response belongs in relation to a complete resource.",
                Example = JsonValue.Create("bytes 0-499/999")
            });
            EnrichResponseHeaders(partialContent, true);

            operation.Responses.TryAdd(StatusCodes.Status206PartialContent.ToString(), partialContent);
        }

        OpenApiResponse notModified = new() { Description = "Returned when conditions specified in one of the if-* headers were not meet." };
        EnrichResponseHeaders(notModified, false);

        operation.Responses.TryAdd(StatusCodes.Status304NotModified.ToString(), notModified);
    }

    private static void EnrichResponseHeaders(IOpenApiResponse response, bool binaryResponse)
    {
        if (binaryResponse)
        {
            response.Headers?.Add(HeaderNames.AcceptRanges, new OpenApiHeader
            {
                Description = "Contains the supported range units.",
                Example = JsonValue.Create("bytes")
            });
            response.Headers?.Add(HeaderNames.ContentLength, new OpenApiHeader
            {
                Description = "The amount of bytes the response has.",
                Example = JsonValue.Create(999)
            });
        }

        response.Headers?.Add(HeaderNames.ETag, new OpenApiHeader
        {
            Description = "Is an identifier for a specific version of a resource.",
        });
        response.Headers?.Add(HeaderNames.LastModified, new OpenApiHeader
        {
            Description = "Contains the date and time when the resource was last modified.",
            Example = _dateTimeExample
        });
    }
}
