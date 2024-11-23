using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using SchulCloud.RestApi.ActionFilters;
using SchulCloud.RestApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Reflection;

namespace SchulCloud.RestApi.Swagger;

internal class PaginationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Attribute? paginationAttribute = context.MethodInfo.GetCustomAttributes().FirstOrDefault(IsPaginationAttribute);
        if (paginationAttribute is not null)
        {
            (int defaultPage, int defaultPageSize, int[] statusCodes) = GetAttributeData(paginationAttribute);

            operation.Parameters.Add(new()
            {
                Name = "page",
                Description = "The index of the page to return. This value have to be greater or same than 0.",
                In = ParameterLocation.Query,
                Schema = context.SchemaGenerator.GenerateSchema(typeof(int), context.SchemaRepository),
                Example = OpenApiAnyFactory.CreateFromJson(defaultPage.ToString()),
            });
            operation.Parameters.Add(new()
            {
                Name = "pageSize",
                Description = "The count of items that are returned per page. This value have to be greater or same than 1.",
                In = ParameterLocation.Query,
                Schema = context.SchemaGenerator.GenerateSchema(typeof(int), context.SchemaRepository),
                Example = OpenApiAnyFactory.CreateFromJson(defaultPageSize.ToString())
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
            Type targetType = typeof(PaginationFilter<>).MakeGenericType(attributeType.GenericTypeArguments[0]);
            return targetType == attributeType;
        }

        return false;
    }

    private static (int PageIndex, int PageSize, int[] StatusCodes) GetAttributeData(Attribute attribute)
    {
        PropertyInfo pageProperty = attribute.GetType().GetProperty("PageIndex")!;
        int page = (int)pageProperty.GetValue(attribute)!;

        PropertyInfo pageSizeProperty = attribute.GetType().GetProperty("PageSize")!;
        int pageSize = (int)pageSizeProperty.GetValue(attribute)!;

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
