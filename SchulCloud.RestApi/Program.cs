using Asp.Versioning.ApiExplorer;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Options;
using SchulCloud.Authentication;
using SchulCloud.Authorization.Extensions;
using SchulCloud.Database;
using SchulCloud.Database.Extensions;
using SchulCloud.Database.Models;
using SchulCloud.RestApi.Extensions;
using SchulCloud.RestApi.Options;
using SchulCloud.RestApi.Swagger;
using SchulCloud.ServiceDefaults;
using SchulCloud.Store;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SchulCloud.RestApi;

internal class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.AddAspirePostgresDb<SchulCloudDbContext>(ResourceNames.IdentityDatabase);

        builder.Services.AddIdentityCore<SchulCloudUser>()
            .AddRoles<SchulCloudRole>()
            .AddSchulCloudEntityFrameworkStores<SchulCloudDbContext>()
            .AddSchulCloudManagers();
        builder.ConfigureManagers();

        builder.Services.AddAuthentication(SchemeNames.ApiKeyScheme)
            .AddApiKey<SchulCloudUser>();
        builder.Services.AddAuthorizationBuilder()
            .AddPermissionsPolicies();

        builder.Services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssemblyContaining<IRestApi>();

        builder.Services.AddControllers();

        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        builder.Services.AddSwaggerGen();
        builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwagger>();

        builder.Services.Configure<OpenApiOptions>(builder.Configuration.GetSection("OpenApi"));

        WebApplication app = builder.Build();
        app.MapDefaultEndpoints();
        app.UseForwardedHeaders();
        app.UseTraceHeader();

        app.UseAuthentication();
        app.UseAuthorization();

        if (!app.Environment.IsDevelopment())
        {
            app.UseProductionExceptionHandler();
        }

        app.MapControllers().RequireAuthorization();

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

        app.Run();
    }
}
