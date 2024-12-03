using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics.CodeAnalysis;

namespace SchulCloud.RestApi.Swagger;

/// <summary>
/// Extensions for <see cref="ISchemaGenerator"/>.
/// </summary>
public static class SchemaGeneratorExtensions
{
    /// <summary>
    /// Generates a string schema with a RegEx pattern.
    /// </summary>
    /// <param name="generator">The generator to use.</param>
    /// <param name="repository">The repository to use.</param>
    /// <param name="pattern">The regular expression</param>
    /// <returns>The generated schema.</returns>
    public static OpenApiSchema GenerateSchemaStringWithPattern(
        this ISchemaGenerator generator,
        SchemaRepository repository,
        [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        OpenApiSchema schema = generator.GenerateSchema(typeof(string), repository);
        schema.Pattern = pattern;
        return schema;
    }
}
