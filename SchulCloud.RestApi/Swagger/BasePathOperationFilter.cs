using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SchulCloud.RestApi.Swagger;

internal class BasePathOperationFilter(IConfiguration configuration) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        string basePath = configuration["BasePath"] ?? string.Empty;
        context.ApiDescription.RelativePath = $"{basePath.TrimEnd('/')}/{context.ApiDescription.RelativePath?.TrimStart('/')}";
    }
}
