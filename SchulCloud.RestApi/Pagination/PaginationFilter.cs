using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace SchulCloud.RestApi.Pagination;

internal class PaginationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();
        
        Attribute? paginationAttribute = context.MethodInfo.GetCustomAttributes().FirstOrDefault(IsPaginationAttribute);
        if (paginationAttribute is null)
            return;
        
        (int defaultOffset, int defaultLimit, int[] statusCodes) = GetAttributeData(paginationAttribute);
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "offset",
            Description = "The offset to apply on the returned collection. This value have to be greater or same than 0.",
            In = ParameterLocation.Query,
            Schema = context.SchemaGenerator.GenerateSchema(typeof(int), context.SchemaRepository),
            Example = JsonValue.Create(defaultOffset),
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "limit",
            Description = "The maximum amount of items to return per request. This value have to be greater or same than 1.",
            In = ParameterLocation.Query,
            Schema = context.SchemaGenerator.GenerateSchema(typeof(int), context.SchemaRepository),
            Example = JsonValue.Create(defaultLimit)
        });

        if (operation.Responses is null)
            return;
        foreach (OpenApiMediaType type in operation.Responses
                     .Where(kvp => IsInStatusCodeRange(kvp.Key, statusCodes) && kvp.Value.Content is not null)
                     .SelectMany(kvp => kvp.Value.Content!.Select(content => content.Value)))
        {
            Type itemType = paginationAttribute.GetType().GenericTypeArguments[0];
            Type responseTyp = typeof(PagingInfo<>).MakeGenericType(itemType);

            type.Schema = context.SchemaGenerator.GenerateSchema(responseTyp, context.SchemaRepository);
        }
    }

    private static bool IsPaginationAttribute(Attribute attribute)
    {
        Type attributeType = attribute.GetType();
        if (attributeType.GenericTypeArguments.Length == 1)
        {
            Type targetType = typeof(PaginationFilterAttribute<>).MakeGenericType(attributeType.GenericTypeArguments[0]);
            return targetType == attributeType;
        }
        return false;
    }

    private static (int PageIndex, int PageSize, int[] StatusCodes) GetAttributeData(Attribute attribute)
    {
        PropertyInfo offsetProperty = attribute.GetType().GetProperty("Offset")!;
        var page = (int)offsetProperty.GetValue(attribute)!;

        PropertyInfo limitProperty = attribute.GetType().GetProperty("Limit")!;
        var pageSize = (int)limitProperty.GetValue(attribute)!;

        PropertyInfo statusCodesProperty = attribute.GetType().GetProperty("StatusCodes")!;
        int[] statusCodes = (int[]?)statusCodesProperty.GetValue(attribute) ?? Enumerable.Range(200, 100).ToArray();

        return (page, pageSize, statusCodes);
    }

    private static bool IsInStatusCodeRange(string code, int[] statusCodes)
    {
        IEnumerable<string> statusCodeStr = statusCodes.Select(c => c.ToString());
        return statusCodeStr.Contains(code);
    }
}
