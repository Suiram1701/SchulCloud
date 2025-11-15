using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace SchulCloud.RestApi.Sorting;

internal class SortingFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();
        
        Attribute? sortingAttribute = context.MethodInfo.GetCustomAttributes().FirstOrDefault(IsSortingAttribute);
        if (sortingAttribute is not null)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "sort",
                Description = "A parameter used for sorting results. Accepts multiple fields, prioritized by their order in the list. " +
                              "Use the name of the field to specify the field to sort by leading with **+** or nothing for ascending (default) and **-** for descending. " +
                              "Its allowed to define this parameter multiple times.",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Pattern = "^([+-]?[a-zA-Z]+)(?:,([+-]?[a-zA-Z]+))*$"
                },
                Example = JsonValue.Create("-Name,+Id")
            });
        }
    }

    private static bool IsSortingAttribute(Attribute attribute)
    {
        Type attributeType = attribute.GetType();
        if (attributeType.GenericTypeArguments.Length == 1)
        {
            Type targetType = typeof(PaginationFilterAttribute<>).MakeGenericType(attributeType.GenericTypeArguments[0]);
            return targetType == attributeType;
        }

        return false;
    }
}
