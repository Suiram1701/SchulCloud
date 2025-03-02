using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SchulCloud.Authorization;
using SchulCloud.Authorization.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Data;
using System.Reflection;

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

        bool added = false;
        foreach (Type responseType in responseTypes)
        {
            foreach (PropertyInfo property in responseType.GetProperties())
            {
                RequireFieldPermissionAttribute[] propPermissions = [.. property.GetCustomAttributes<RequireFieldPermissionAttribute>()];
                added = actionPermissions.Length == 0
                    ? propPermissions.Length > 0
                    : propPermissions.Any(attribute => actionPermissions.Any(permission => permission.Name == attribute.Name && permission.Level < attribute.Level));
                if (added)
                {
                    operation.Description ??= string.Empty;

                    if (!operation.Description.EndsWith('.'))
                        operation.Description += ". ";
                    operation.Description += "Some fields of the response require greater permissions than calling this endpoint. See the field's descriptions for further information.";

                    break;
                }
            }

            if (added)
                break;
        }
    }

    /// <inheritdoc />
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach ((string name, OpenApiSchema schema) in swaggerDoc.Components.Schemas)
        {
            if (GetTypeBySchemaName(name) is not Type schemaType)
                continue;

            Dictionary<string, PermissionLevel> highestLevels = [];
            foreach ((string schemaName, OpenApiSchema propertySchema) in schema.Properties)
            {
                PropertyInfo? property = MapSchemaPropertyToProperty(schemaType, schemaName);
                if (property?.GetCustomAttribute<RequireFieldPermissionAttribute>() is RequireFieldPermissionAttribute attribute)
                {
                    if (!propertySchema.Description.EndsWith('.'))
                        propertySchema.Description += ". ";

                    propertySchema.Nullable = true;
                    propertySchema.Description +=
                        $"This field will only be returned if the request was made with the permission **{attribute.Name}** at the level **{attribute.Level}** or greater. " +
                        "If the required permissions are not met, **null** will be returned.";
                }
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
}
