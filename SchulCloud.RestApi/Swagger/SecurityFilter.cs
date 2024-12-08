using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SchulCloud.Authorization.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net.Mime;
using System.Reflection;

namespace SchulCloud.RestApi.Swagger;

internal class SecurityFilter : IOperationFilter
{
    private const string _unauthorizedExample = """
        {
            "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            "title": "Unauthorized",
            "status": 401,
            "detail": "An API key is required to call this endpoint.",
            "traceId": ""
        }
        """;

    private const string _forbiddenExample = """
        {
            "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            "title": "Forbidden",
            "status": 403,
            "detail": "The used API key does not have the privileges to call this endpoint.",
            "traceId": ""
        }
        """;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        OpenApiResponse unauthorizedResponse = new()
        {
            Description = "No API key were provided in the request.",
            Content = GetProblemMediaType(context, _unauthorizedExample)
        };
        operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), unauthorizedResponse);

        IEnumerable<RequirePermissionAttribute> permissionAttributes = context.MethodInfo.GetCustomAttributes<RequirePermissionAttribute>();
        if (permissionAttributes.Any())
        {
            foreach (RequirePermissionAttribute permission in permissionAttributes)
            {
                if (!(operation.Description?.EndsWith("\r\n") ?? true))
                {
                    operation.Description += "\r\n";
                }
                operation.Description += $"Requires the permission **{permission.Name}** with level **{permission.Level}** or greater.";
            }

            OpenApiResponse response = new()
            {
                Description = "The used API key does not have the privileges to call this endpoint.",
                Content = GetProblemMediaType(context, _forbiddenExample),
            };
            operation.Responses.Add(StatusCodes.Status403Forbidden.ToString(), response);
        }
    }

    private static Dictionary<string, OpenApiMediaType> GetProblemMediaType(OperationFilterContext context, string exampleJson)
    {
        OpenApiMediaType mediaType = new()
        {
            Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository),
            Example = OpenApiAnyFactory.CreateFromJson(exampleJson)
        };
        return new Dictionary<string, OpenApiMediaType>()
        {
            { MediaTypeNames.Application.ProblemJson, mediaType}
        };
    }
}
