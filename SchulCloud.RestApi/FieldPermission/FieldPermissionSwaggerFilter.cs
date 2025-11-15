using Microsoft.AspNetCore.Mvc;
using SchulCloud.Authorization.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Microsoft.OpenApi;

namespace SchulCloud.RestApi.FieldPermission;

/// <summary>
/// A document filter that will apply documentation to fields that need authorization.
/// </summary>
public class FieldPermissionSwaggerFilter : IOperationFilter, IDocumentFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        RequirePermissionAttribute[] actionPermissions = [.. context.MethodInfo.GetCustomAttributes<RequirePermissionAttribute>()];

        IEnumerable<Type> responseTypes = context.MethodInfo
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Where(attribute => attribute.StatusCode is >= 200 and < 300)
            .Select(attribute => attribute.Type);

        var added = false;
        foreach (Type responseType in responseTypes)
        {
            foreach (PropertyInfo property in responseType.GetProperties())
            {
                RequireFieldPermissionAttribute[] propPermissions = [.. property.GetCustomAttributes<RequireFieldPermissionAttribute>()];
                added = actionPermissions.Length == 0
                    ? propPermissions.Length > 0
                    : propPermissions.Any(attribute => actionPermissions.Any(permission => permission.Name == attribute.Name && permission.Level < attribute.Level));
                if (!added)
                    continue;
                
                operation.Description ??= string.Empty;
                if (!operation.Description.EndsWith('.'))
                    operation.Description += ". ";
                operation.Description += "Some fields of the response require greater permissions than calling this endpoint. See the field's descriptions for further information.";
                
                break;
            }

            if (added)
                break;
        }
    }

    /// <inheritdoc />
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach ((string name, IOpenApiSchema schema) in swaggerDoc.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>())
        {
            if (GetTypeBySchemaName(name) is not { } schemaType)
                continue;

            foreach ((string schemaName, IOpenApiSchema propertySchema) in schema.Properties ?? new Dictionary<string, IOpenApiSchema>())
            {
                PropertyInfo? property = MapSchemaPropertyToProperty(schemaType, schemaName);
                if (property?.GetCustomAttribute<RequireFieldPermissionAttribute>() is not { } attribute)
                    continue;
                
                OpenApiSchema schemaInstance = GetRealInstance(propertySchema, context.SchemaRepository);
                if (!(schemaInstance.Description?.EndsWith('.') ?? true))
                    schemaInstance.Description += ". ";

                schemaInstance.Type |= JsonSchemaType.Null;
                schemaInstance.Description +=
                    $"This field will only be returned if the request was made with the permission **{attribute.Name}** at the level **{attribute.Level}** or greater. " +
                    "If the required permissions are not met, **null** will be returned.";
            }
        }
    }

    private static Type? GetTypeBySchemaName(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.ExportedTypes)
            .FirstOrDefault(type => type.Name == name);
    }

    private static PropertyInfo? MapSchemaPropertyToProperty(Type modelType, string schemaProperty)
    {
        return modelType
            .GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, schemaProperty, StringComparison.OrdinalIgnoreCase));
    }

    private static OpenApiSchema GetRealInstance(IOpenApiSchema schema, SchemaRepository schemaRepository)
    {
        while (true)
        {
            if (schema is OpenApiSchema apiSchema)
                return apiSchema;
            schema = schemaRepository.Schemas[schema.Id ?? throw new InvalidOperationException("An Id is required for a schema!")];
        }
    }
}
