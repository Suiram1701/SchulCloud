using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace SchulCloud.RestApi.RequireSelfOrPermission;

/// <summary>
/// An operation filter used for generating documentation for <see cref="RequireSelfOrPermissionAttribute"/>.
/// </summary>
public class RequireSelfOrPermissionFilter : IOperationFilter
{
    private const string _forbiddenExample = """
        {
            "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            "title": "Forbidden",
            "status": 403,
            "detail": "The used API key does not have the privileges to access this resource.",
            "traceId": ""
        }
        """;

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attribute = context.MethodInfo.GetCustomAttribute<RequireSelfOrPermissionAttribute>();
        if (attribute is not null)
        {
            operation.Description ??= string.Empty;
            if (!operation.Description.EndsWith("\r\n"))
                operation.Description += "\r\n";

            operation.Description += 
                $"To call this endpoint the parameter '{attribute.SelfParameter}' have to match with the ID of the user owning the used API key " +
                $"or the permission **{attribute.PermissionName}** with level **{attribute.PermissionLevel}** or greater have to be available.";

            IOpenApiResponse errorResponse = (operation.Responses ??= new OpenApiResponses())[StatusCodes.Status403Forbidden.ToString()];
            errorResponse.Description = "The requirements to call to call this endpoint were not meet.";
            errorResponse.Content?[Application.ProblemJson].Example = JsonNode.Parse(_forbiddenExample);
        }
    }
}
