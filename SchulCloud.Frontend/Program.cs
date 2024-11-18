using Blazored.LocalStorage;
using MailKit.Client;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;
using MyCSharp.HttpUserAgentParser.DependencyInjection;
using SchulCloud.Database;
using SchulCloud.Database.Extensions;
using SchulCloud.ServiceDefaults;
using SchulCloud.Store;
using SchulCloud.Frontend.Components;
using SchulCloud.Frontend.Extensions;
using SchulCloud.Frontend.Identity;
using SchulCloud.Frontend.Identity.EmailSenders;
using SchulCloud.Frontend.Identity.Managers;
using SchulCloud.Frontend.Services;
using SchulCloud.Frontend.Services.Interfaces;
using SchulCloud.Authorization.Extensions;
using MudBlazor.Translations;
using SchulCloud.Frontend.HostedServices;

namespace SchulCloud.Frontend;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.ConfigureOptions();

        builder.Services.AddMemoryCache();

        builder.AddAspirePostgresDb<SchulCloudDbContext>(ResourceNames.IdentityDatabase);
        builder.AddMailKitClient(ResourceNames.MailServer);

        IdentityBuilder identityBuilder = builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddSchulCloudEntityFrameworkStores<SchulCloudDbContext>()
            .ConfigureDefaultIdentityCookies()
            .AddSchulCloudManagers()
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
        builder.Services.AddCascadingAuthenticationState();

        builder.Services
            .AddMudServices()
            .AddMudTranslations()
            .AddBlazoredLocalStorage()
            .AddScoped<CookieService>()
            .AddScoped<CookieConsentService>()
            .AddScoped<IUserPreferencesStore, CookieUserPreferencesStore>();

        builder.Services
            .AddSingleton<IIPGeolocator, IPApiGeolocator>()
            .AddSingleton<LoginAttemptLoggingService>()
            .AddHostedService(sp => sp.GetRequiredService<LoginAttemptLoggingService>());
        builder.AddConfiguredGoogleMapsServices();

        WebApplication app = builder.Build();
        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStaticFiles();

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
