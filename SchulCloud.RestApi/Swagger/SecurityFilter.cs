using Microsoft.AspNetCore.Mvc;
using SchulCloud.Authorization.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace SchulCloud.RestApi.Swagger;

internal class SecurityFilter : IOperationFilter
{
    private const string _unauthorizedExample = """
        {
            "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            "title": "Unauthorized",
            "status": 401,
            "detail": "An API key is required to access this resource.",
            "traceId": ""
        }
        """;

    private const string _forbiddenExample = """
        {
            "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            "title": "Forbidden",
            "status": 403,
            "detail": "The used API key does not have the privileges to access this resource.",
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
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), unauthorizedResponse);

        RequirePermissionAttribute[] permissionAttributes = [.. context.MethodInfo.GetCustomAttributes<RequirePermissionAttribute>()];
        if (permissionAttributes.Length <= 0)
            return;
        
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

    private static Dictionary<string, OpenApiMediaType> GetProblemMediaType(OperationFilterContext context, string exampleJson)
    {
        OpenApiMediaType mediaType = new()
        {
            Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository),
            Example = JsonNode.Parse(exampleJson)
        };
        return new Dictionary<string, OpenApiMediaType>()
        {
            { Application.ProblemJson, mediaType}
        };
    }
}
