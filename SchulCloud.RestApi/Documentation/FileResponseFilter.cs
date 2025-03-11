using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SchulCloud.RestApi.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SchulCloud.RestApi.Documentation;

internal sealed class FileResponseFilter : AttributeFilterBase<ProducesFileResponseAttribute>
{
    private static readonly OpenApiString _dateTimeExample = new("Mon, 01 Jan 2020 00:00:00 GMT");

    protected override void Apply(OpenApiOperation operation, OperationFilterContext context, ProducesFileResponseAttribute[] attributes)
    {
        ProducesFileResponseAttribute attribute = attributes[0];     // Only one is allowed.

        if (attribute.SupportsRangeProcessing)
        {
            operation.Parameters.Add(new()
            {
                Name = HeaderNames.Range,
                In = ParameterLocation.Header,
                Description = "Indicates the part of the resource that should be returned.",
                Example = new OpenApiString("bytes=0-499"),
                Schema = context.SchemaGenerator.GenerateSchemaStringWithPattern(context.SchemaRepository, @"^bytes=(\d*)-(\d*)$")
            });
        }

        // Conditional headers
        operation.Parameters.Add(new()
        {
            Name = HeaderNames.IfMatch,
            In = ParameterLocation.Header,
            Description = "The resource will only be returned if the ETag matches the provided one."
        });
        operation.Parameters.Add(new()
        {
            Name = HeaderNames.IfNoneMatch,
            In = ParameterLocation.Header,
            Description = "The resource will only be returned if the ETag doesn't matches the provided one."
        });
        operation.Parameters.Add(new()
        {
            Name = HeaderNames.IfModifiedSince,
            In = ParameterLocation.Header,
            Description = "Ensures the resource is only returned if it was modified since the specific date time.",
            Example = _dateTimeExample
        });
        operation.Parameters.Add(new()
        {
            Name = HeaderNames.IfUnmodifiedSince,
            In = ParameterLocation.Header,
            Description = "Ensures the resource is only returned if it wasn't modified since the specific date time.",
            Example = _dateTimeExample
        });

        OpenApiResponse ok = operation.Responses[StatusCodes.Status200OK.ToString()];
        foreach (string type in attribute.ContentTypes)
        {
            ok.Content.TryAdd(type, new());
            ok.Content[type].Schema = context.SchemaGenerator.GenerateBinarySchema(context.SchemaRepository);
        }
        EnrichResponseHeaders(operation.Responses[StatusCodes.Status200OK.ToString()], attribute.SupportsRangeProcessing);

        if (attribute.SupportsRangeProcessing)
        {
            OpenApiResponse partialContent = new()
            {
                Description = "Returned when only the requested part of the resource were returned.",
                Content = attribute.ContentTypes.ToDictionary(
                    keySelector: value => value,
                    elementSelector: value => new OpenApiMediaType { Schema = context.SchemaGenerator.GenerateBinarySchema(context.SchemaRepository) }),
                Headers =
                {
                    {
                        HeaderNames.ContentRange, new OpenApiHeader
                        {
                            Description = "Indicates where the content of a response belongs in relation to a complete resource.",
                            Example = new OpenApiString("bytes 0-499/999")
                        }
                    }
                }
            };
            EnrichResponseHeaders(partialContent, true);

            operation.Responses.TryAdd(StatusCodes.Status206PartialContent.ToString(), partialContent);
        }

        OpenApiResponse notModified = new() { Description = "Returned when conditions specified in one of the if-* headers were not meet." };
        EnrichResponseHeaders(notModified, false);

        operation.Responses.TryAdd(StatusCodes.Status304NotModified.ToString(), notModified);
    }

    private static void EnrichResponseHeaders(OpenApiResponse response, bool binaryResponse)
    {
        if (binaryResponse)
        {
            response.Headers.Add(HeaderNames.AcceptRanges, new()
            {
                Description = "Contains the supported range units.",
                Example = new OpenApiString("bytes")
            });
            response.Headers.Add(HeaderNames.ContentLength, new()
            {
                Description = "The amount of bytes the response has.",
                Example = new OpenApiInteger(999)
            });
        }

        response.Headers.Add(HeaderNames.ETag, new()
        {
            Description = "Is an identifier for a specific version of a resource.",
        });
        response.Headers.Add(HeaderNames.LastModified, new()
        {
            Description = "Contains the date and time when the resource was last modified.",
            Example = _dateTimeExample
        });
    }
}
