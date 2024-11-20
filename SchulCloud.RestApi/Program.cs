using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using SchulCloud.RestApi.Options;
using SchulCloud.RestApi.Swagger;
using SchulCloud.ServiceDefaults;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SchulCloud.RestApi;

internal class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddControllers();

        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        builder.Services.AddSwaggerGen(options =>
        {
            options.OperationFilter<BasePathOperationFilter>();
        });
        builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwagger>();

        builder.Services.Configure<OpenApiOptions>(builder.Configuration.GetSection("OpenApi"));

        WebApplication app = builder.Build();
        app.MapControllers();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            string basePath = app.Configuration["BasePath"] ?? string.Empty;

            foreach (ApiVersionDescription apiVersion in app.DescribeApiVersions())
            {
                options.SwaggerEndpoint(
                    url: $"{basePath.TrimEnd('/')}/swagger/{apiVersion.GroupName}/swagger.json",
                    name: apiVersion.GroupName.ToUpperInvariant());
            }
        });

        app.MapDefaultEndpoints();

        app.Run();
    }
}
