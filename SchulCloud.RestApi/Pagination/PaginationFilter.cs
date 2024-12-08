using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SchulCloud.RestApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Reflection;

namespace SchulCloud.RestApi.Pagination;

internal class PaginationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Attribute? paginationAttribute = context.MethodInfo.GetCustomAttributes().FirstOrDefault(IsPaginationAttribute);
        if (paginationAttribute is not null)
        {
            (int defaultOffset, int defaultLimit, int[] statusCodes) = GetAttributeData(paginationAttribute);

            operation.Parameters.Add(new()
            {
                Name = "offset",
                Description = "The offset to apply on the returned collection. This value have to be greater or same than 0.",
                In = ParameterLocation.Query,
                Schema = context.SchemaGenerator.GenerateSchema(typeof(int), context.SchemaRepository),
                Example = new OpenApiInteger(defaultOffset),
            });
            operation.Parameters.Add(new()
            {
                Name = "limit",
                Description = "The maximum amount of items to return per request. This value have to be greater or same than 1.",
                In = ParameterLocation.Query,
                Schema = context.SchemaGenerator.GenerateSchema(typeof(int), context.SchemaRepository),
                Example = new OpenApiInteger(defaultLimit)
            });

            foreach (OpenApiMediaType type in operation.Responses
                .Where(kvp => IsInStatusCodeRange(kvp.Key, statusCodes))
                .SelectMany(kvp => kvp.Value.Content.Select(content => content.Value)))
            {
                Type itemType = paginationAttribute.GetType().GenericTypeArguments[0];
                Type responseTyp = typeof(PagingInfo<>).MakeGenericType(itemType);

                type.Schema = context.SchemaGenerator.GenerateSchema(responseTyp, context.SchemaRepository);
            }
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
        int page = (int)offsetProperty.GetValue(attribute)!;

        PropertyInfo limitProperty = attribute.GetType().GetProperty("Limit")!;
        int pageSize = (int)limitProperty.GetValue(attribute)!;

        PropertyInfo statusCodesProperty = attribute.GetType().GetProperty("StatusCodes")!;
        int[] statusCodes = (int[]?)statusCodesProperty.GetValue(attribute) ?? Enumerable.Range(200, 100).ToArray();

        return (page, pageSize, statusCodes);
    }

    private static bool IsInStatusCodeRange(string code, int[] statusCodes)
    {
        IEnumerable<string> statusCodeStr = statusCodes.Select(code => code.ToString());
        return statusCodeStr.Contains(code);
    }
}
