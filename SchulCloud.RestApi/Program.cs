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
using Swashbuckle.AspNetCore.SwaggerUI;

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

        if (builder.Configuration.GetValue<bool?>("Swagger:Enabled") ?? false)
        {
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<OpenApiOptions>(builder.Configuration.GetSection("OpenApi"));
            builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwagger>();
        }
        if (builder.Configuration.GetValue<bool?>("Swagger:UiEnabled") ?? false)
        {
            builder.Services.AddTransient<IConfigureOptions<SwaggerUIOptions>, ConfigureSwaggerUI>();
        }

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

        if (app.Configuration.GetValue<bool?>("Swagger:Enabled") ?? false)
        {
            app.UseSwagger();
        }
        if (app.Configuration.GetValue<bool?>("Swagger:UiEnabled") ?? false)
        {
            app.UseSwaggerUI();
        }

        app.Run();
    }
}
