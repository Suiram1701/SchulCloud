using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SchulCloud.RestApi.ActionFilters;
using SchulCloud.RestApi.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace SchulCloud.RestApi.Filtering;

internal class FilteringOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Attribute? filteringAttribute = context.MethodInfo.GetCustomAttributes().FirstOrDefault(IsFilteringAttribute);
        if (filteringAttribute is not null)
        {
            operation.Parameters.Add(new()
            {
                Name = "filter",
                Description = "A parameter used for filtering results. Accepts multiple filters that will be connected using AND. " +
                              "Use the name of the field followed by a **:** and the value it has to equal to (example: *Name:Example*). " +
                              "Use **[**operator**]** to specify an operator to use. Possible operators are: **eq** (equals), **ne** (not equals), " +
                              "**gt** (greater than), **lt** (less than), **gte** (greater or equal than), **lte** (less or equal than), " +
                              "**like** (string contains),**ilike** (case-insensitive string contains). Only specific operators are allowed depending on the fields type, its logically understandable which operator is allowed for which type. " +
                              "String values to compare the fields against have to be URL encoded. An empty value for an non-string field will be interpret as NULL otherwise its still an empty string. " +
                              "Use **,** to define multiple conditions or define this parameter multiple times.",
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
            Type targetType = typeof(PaginationFilter<>).MakeGenericType(attributeType.GenericTypeArguments[0]);
            return targetType == attributeType;
        }

        return false;
    }
}
