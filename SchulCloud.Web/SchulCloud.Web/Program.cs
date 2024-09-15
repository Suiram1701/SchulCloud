using Blazored.LocalStorage;
using MailKit.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SchulCloud.Database;
using SchulCloud.Database.Extensions;
using SchulCloud.ServiceDefaults;
using SchulCloud.Store;
using SchulCloud.Web.Components;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Identity;
using SchulCloud.Web.Identity.EmailSenders;
using SchulCloud.Web.Identity.Managers;
using SchulCloud.Web.Services;
using SchulCloud.Web.Services.Interfaces;

namespace SchulCloud.Web;

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
            string connectionString = builder.Configuration.GetConnectionString("schulcloud-db")
                ?? throw new InvalidOperationException("A connection string to the database have to be provided.");

            options.UseNpgsql(connectionString);

            if (builder.Environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });
        builder.EnrichNpgsqlDbContext<SchulCloudDbContext>();

        builder.AddMailKitClient("maildev");

        IdentityBuilder identityBuilder = builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddSchulCloudEntityFrameworkStores<SchulCloudDbContext>()
            .AddSchulCloudManagers<AppCredential>()
            .AddSignInManager<SchulCloudSignInManager<ApplicationUser, AppCredential>>()
            .AddErrorDescriber<LocalizedErrorDescriber>()
            .AddEmailSender<MailKitEmailSender<ApplicationUser>>()
            .AddRequestLimiter<CachedRequestLimiter<ApplicationUser>>()
            .AddDefaultTokenProviders()
            .AddTokenProviders();

        builder.Services.AddFido2Services();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = Routes.SignIn();
            options.LogoutPath = Routes.SignOut();
            options.AccessDeniedPath = Routes.ErrorIndex(StatusCodes.Status403Forbidden);

            options.ReturnUrlParameter = "returnUrl";

            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
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
            .AddScoped<IUserPreferencesStore, CookieUserPreferencesStore>();

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
