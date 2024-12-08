using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using SchulCloud.ServiceDefaults.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SchulCloud.RestApi.Swagger;

internal class ConfigureSwaggerUI(IOptionsSnapshot<ServiceOptions> optionsAccessor, IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerUIOptions>
{
    public void Configure(SwaggerUIOptions options)
    {
        foreach (ApiVersionDescription apiVersion in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                url: optionsAccessor.Value.GetPathWithBase($"/swagger/{apiVersion.GroupName}/swagger.json"),
                name: apiVersion.GroupName.ToUpperInvariant());
        }
    }
}
