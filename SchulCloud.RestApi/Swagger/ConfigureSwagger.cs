﻿using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SchulCloud.RestApi.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SchulCloud.RestApi.Swagger;

internal class ConfigureSwagger(IApiVersionDescriptionProvider provider, IOptions<OpenApiOptions> optionsAccessor) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (ApiVersionDescription apiVersion in provider.ApiVersionDescriptions)
        {
            OpenApiInfo versionInfo = optionsAccessor.Value.CreateOpenApiInfo();
            versionInfo.Version = apiVersion.ApiVersion.ToString();

            options.SwaggerDoc(apiVersion.GroupName, versionInfo);
        }

        options.IncludeXmlComments(typeof(IRestApi).Assembly, includeControllerXmlComments: true);

        options.AddSecurityDefinition("API key", new()
        {
            Type = SecuritySchemeType.ApiKey,
            Description = "An API key generated by a user on the web frontend.",
            Name = "x-api-key",
            In = ParameterLocation.Header
        });

        options.OperationFilter<BasePathOperationFilter>();
        options.OperationFilter<SecurityResponsesOperationFilter>();
        options.OperationFilter<PaginationOperationFilter>();
    }
}
