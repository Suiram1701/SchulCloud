using AwsS3.Client;
using Blazored.LocalStorage;
using MailKit.Client;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;
using MudBlazor.Translations;
using MyCSharp.HttpUserAgentParser.DependencyInjection;
using OpenTelemetry.Trace;
using Quartz;
using Quartz.AspNetCore;
using SchulCloud.Authorization.Extensions;
using SchulCloud.Database;
using SchulCloud.Database.Extensions;
using SchulCloud.FileStorage.S3;
using SchulCloud.Frontend.CircuitHandlers;
using SchulCloud.Frontend.Components;
using SchulCloud.Frontend.Extensions;
using SchulCloud.Frontend.Identity;
using SchulCloud.Frontend.Identity.EmailSenders;
using SchulCloud.Frontend.Identity.Managers;
using SchulCloud.Frontend.Jobs;
using SchulCloud.Frontend.JsInterop;
using SchulCloud.Frontend.Services;
using SchulCloud.Frontend.Services.Interfaces;
using SchulCloud.Identity;
using SchulCloud.Identity.Services;
using SchulCloud.ServiceDefaults;

namespace SchulCloud.Frontend;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder
            .AddServiceDefaults()
            .ConfigureIdentity()
            .ConfigureOptions();

        builder.Services.AddMemoryCache();

        builder.AddAspirePostgresDb<AppDbContext>(ResourceNames.IdentityDatabase, pooledService: false);
        builder.AddMinIOAwsClient(ResourceNames.MinIOStorage);
        builder.AddMailKitClient(ResourceNames.MailServer);

        IdentityBuilder identityBuilder = builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddSchulCloudEntityFrameworkStores<AppDbContext>()
            .AddS3ProfileImageStorage()
            .ConfigureDefaultIdentityCookies()
            .AddManagers()
            .AddApiKeysService<ApiKeyService>()
            .AddSignInManager<SchulCloudSignInManager>()
            .AddErrorDescriber<LocalizedErrorDescriber>()
            .AddEmailSender<MailKitEmailSender<ApplicationUser>>()
            .AddRequestLimiter<CachedRequestLimiter<ApplicationUser>>()
            .AddDefaultTokenProviders()
            .AddTokenProviders();

        builder.Services.AddAuthorizationBuilder()
            .AddPermissionsPolicies();

        builder.Services
            .AddFido2Services()
            .AddHttpUserAgentParser();

        builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services
            .AddSingleton<CircuitHandler, TelemetryHandler>()
            .AddCascadingAuthenticationState();

        builder.Services
            .AddMudServices()
            .AddMudTranslations()
            .AddBlazoredLocalStorage()
            .AddScoped<CookieService>()
            .AddScoped<CookieConsentService>()
            .AddScoped<IUserPreferencesStore, CookieUserPreferencesStore>()
            .AddScoped<ClipboardInterop>();

        builder.Services.AddSingleton<IIPGeolocator, IPApiGeolocator>();
        builder.AddConfiguredGoogleMapsServices();

        builder.Services.AddQuartz(options =>
        {
            options.AddJob<LoginAttemptProcessJob>(options => options
                .WithIdentity(Jobs.Jobs.LoginAttemptProcessJob)
                .WithDescription("A job that processes login attempts reported from the frontend.")
                .StoreDurably()
            );
        });
        builder.Services.AddQuartzServer(options => options.WaitForJobsToComplete = true);

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddQuartzInstrumentation()
                .AddSource(TelemetryHandler.ActivitySourceName))
            .WithMetrics(metrics => metrics.AddMeter(TelemetryHandler.MeterName));

        WebApplication app = builder.Build();
        app.UseForwardedHeaders();

        app.MapDefaultEndpoints();
        app.MapCommands();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.MapStaticAssets();
        app.UseFaviconRedirect();

        app.UseHttpsRedirection();
        app.UseStatusCodePagesWithReExecute("/error/{0}");

        app.UseAuthentication();
        app.UseAntiforgery();
        app.UseAuthorization();

        app.UseRequestLocalization();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
