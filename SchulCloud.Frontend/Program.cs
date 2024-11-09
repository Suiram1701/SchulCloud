using Blazored.LocalStorage;
using MailKit.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
using GoogleMapsComponents;
using SchulCloud.Authorization.Extensions;

namespace SchulCloud.Frontend;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder
            .AddServiceDefaults()
            .ConfigureOptions();

        builder.Services.AddMemoryCache();

        builder.Services.AddDbContext<SchulCloudDbContext>(options =>
        {
            string connectionString = builder.Configuration.GetConnectionString(ResourceNames.IdentityDatabase)
                ?? throw new InvalidOperationException("A connection string to the database have to be provided.");

            options.UseNpgsql(connectionString);

            if (builder.Environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });
        builder.EnrichNpgsqlDbContext<SchulCloudDbContext>();

        builder.AddMailKitClient(ResourceNames.MailServer);

        IdentityBuilder identityBuilder = builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddSchulCloudEntityFrameworkStores<SchulCloudDbContext>()
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

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = Routes.Login();
            options.LogoutPath = Routes.Logout();
            options.AccessDeniedPath = Routes.Forbidden();

            options.ReturnUrlParameter = "returnUrl";

            options.ExpireTimeSpan = TimeSpan.FromDays(31);     // this time is used for persistent sessions.
            options.SlidingExpiration = true; 
        });

        builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services
            .AddMudServices()
            .AddBlazoredLocalStorage()
            .AddScoped<CookieService>()
            .AddScoped<CookieConsentService>()
            .AddScoped<IUserPreferencesStore, CookieUserPreferencesStore>();

        builder.Services
            .AddSingleton<IIPGeolocator, IPApiGeolocator>()
            .AddSingleton<LoginLogBackgroundService>()
            .AddHostedService(sp => sp.GetRequiredService<LoginLogBackgroundService>());

        string? mapsApiKey = builder.Configuration["GoogleMaps:ApiKey"];
        if (!string.IsNullOrWhiteSpace(mapsApiKey))
        {
            builder.Services.AddBlazorGoogleMaps(mapsApiKey);
        }

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

        app.UseStaticFileServer();

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
