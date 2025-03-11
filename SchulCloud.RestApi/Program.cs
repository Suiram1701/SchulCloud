using AwsS3.Client;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using SchulCloud.Authorization.Extensions;
using SchulCloud.Database;
using SchulCloud.Database.Extensions;
using SchulCloud.Database.Models;
using SchulCloud.FileStorage.S3;
using SchulCloud.Identity;
using SchulCloud.Identity.Services;
using SchulCloud.RestApi.Authentication;
using SchulCloud.RestApi.Extensions;
using SchulCloud.RestApi.Options;
using SchulCloud.RestApi.Swagger;
using SchulCloud.ServiceDefaults;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

namespace SchulCloud.RestApi;

internal class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder
            .AddServiceDefaults()
            .ConfigureIdentity();

        builder.AddAspirePostgresDb<AppDbContext>(ResourceNames.IdentityDatabase);
        builder.AddMinIOAwsClient(ResourceNames.SchulCloudStorage);

        builder.Services.AddIdentityCore<AppUser>()
            .AddRoles<AppRole>()
            .AddSchulCloudEntityFrameworkStores<AppDbContext>()
            .AddS3ProfileImageStorage()
            .AddManagers()
            .AddApiKeysService<ApiKeyService>();
        builder.ConfigureIdentity();

        builder.Services.AddAuthentication(SchemeNames.ApiKeyScheme)
            .AddApiKey<AppUser>();
        builder.Services.AddAuthorizationBuilder()
            .AddPermissionsPolicies();

        builder.Services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssemblyContaining<IRestApi>();

        builder.Services.AddControllers()
            .AddMvcOptions(options => options.Filters.Add<FieldPermissionFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddCustomizedProblemDetails();

        builder.Services
            .AddApiVersioning(options => options.ReportApiVersions = true)
            .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        if (builder.Configuration.GetValue<bool?>("OpenAPI:Enabled") ?? false)
        {
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<OpenApiOptions>(builder.Configuration.GetSection("OpenAPI:Info"));
            builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwagger>();
        }

        WebApplication app = builder.Build();
        app.UseForwardedHeaders();
        app.UseRequestIdHeader();

        app.MapDefaultEndpoints();
        app.MapCommands();

        app.UseAuthentication();
        app.UseAuthorization();

        if (!app.Environment.IsDevelopment())
        {
            app.UseProductionExceptionHandler();
        }

        app.MapControllers().RequireAuthorization();

        if (app.Configuration.GetValue<bool?>("OpenAPI:Enabled") ?? false)
        {
            app.UseSwagger(options => options.RouteTemplate = "/openapi/{documentName}.{extension:regex(^(json|ya?ml)$)}");

            if (app.Configuration.GetValue<bool?>("OpenAPI:UiEnabled") ?? false)
            {
                app.MapScalarApiReference("/openapi/scalar", options => options
                    .WithTitle("SchulCloud - API Reference")
                    .WithOpenApiRoutePattern("/openapi/{documentName}.json")
                    .WithDotNetFlag(true)
                    .WithDownloadButton(true));
            }
        }

        app.Run();
    }
}
