using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SchulCloud.RestApi.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace SchulCloud.RestApi.Filtering;

internal class FilteringFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Attribute? filteringAttribute = context.MethodInfo.GetCustomAttributes().FirstOrDefault(IsFilteringAttribute);
        if (filteringAttribute is not null)
        {
            operation.Parameters.Add(new()
            {
                Name = "filter",
                Description = "A filtering parameter that supports multiple conditions combined with AND. Use **FieldName:Value** for equality checks (e.g., **Name:Example**). " +
                              "Specify operators with [operator] (e.g., [gt] for greater than). Supported operators include **eq**, **ne**, **gt**, **lt**, **gte**, **lte**, **like**, and **ilike**, depending on the field type. " +
                              "String values must be URL-encoded. Empty values for non-string fields are treated as **NULL**; otherwise, they remain empty strings. Use **,** or repeat the parameter to add multiple conditions.",
                In = ParameterLocation.Query,
                Schema = context.SchemaGenerator.GenerateSchemaStringWithPattern(context.SchemaRepository, @"^([a-zA-Z][a-zA-Z0-9]+)(?:\[([a-zA-Z]+)\])?:([a-zA-Z0-9-\._~]*),(([a-zA-Z][a-zA-Z0-9]+)(?:\[([a-zA-Z]+)\])?:([a-zA-Z0-9-\._~]*))*$"),
                Example = new OpenApiString("Name[eq]:Example,Amount[gte]:0")
            });
        }
    }

    private static bool IsFilteringAttribute(Attribute attribute)
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
