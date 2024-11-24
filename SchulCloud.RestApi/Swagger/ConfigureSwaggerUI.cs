using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using SchulCloud.ServiceDefaults.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SchulCloud.RestApi.Swagger;

internal class ConfigureSwaggerUI(IOptionsSnapshot<ServiceOptions> optionsAccessor, IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerUIOptions>
{
    public void Configure(SwaggerUIOptions options)
    {
        string basePath = optionsAccessor.Value.BasePath;
        foreach (ApiVersionDescription apiVersion in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                url: $"{basePath.TrimEnd('/')}/swagger/{apiVersion.GroupName}/swagger.json",
                name: apiVersion.GroupName.ToUpperInvariant());
        }
    }
}
