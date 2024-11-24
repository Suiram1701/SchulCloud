using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SchulCloud.ServiceDefaults.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SchulCloud.RestApi.Swagger;

internal class BasePathOperationFilter(IOptionsMonitor<ServiceOptions> optionsMonitor) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        string basePath = optionsMonitor.CurrentValue.BasePath;
        if (!(context.ApiDescription.RelativePath?.StartsWith(basePath) ?? false))
        {
            context.ApiDescription.RelativePath = $"{basePath.TrimEnd('/')}/{context.ApiDescription.RelativePath?.TrimStart('/')}";
        }
    }
}
