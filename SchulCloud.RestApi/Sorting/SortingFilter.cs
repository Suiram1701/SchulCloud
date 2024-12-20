using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SchulCloud.RestApi.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace SchulCloud.RestApi.Sorting;

internal class SortingFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Attribute? sortingAttribute = context.MethodInfo.GetCustomAttributes().FirstOrDefault(IsSortingAttribute);
        if (sortingAttribute is not null)
        {
            operation.Parameters.Add(new()
            {
                Name = "sort",
                Description = "A parameter used for sorting results. Accepts multiple fields, prioritized by their order in the list. " +
                              "Use the name of the field to specify the field to sort by leading with **+** or nothing for ascending (default) and **-** for descending. " +
                              "Its allowed to define this parameter multiple times.",
                In = ParameterLocation.Query,
                Schema = context.SchemaGenerator.GenerateSchemaStringWithPattern(context.SchemaRepository, "^([+-]?[a-zA-Z]+)(?:,([+-]?[a-zA-Z]+))*$"),
                Example = new OpenApiString("-Name,+Id")
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
