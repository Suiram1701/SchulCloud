using Blazored.LocalStorage;
using FluentValidation;
using MailKit.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database;
using SchulCloud.Database.Models;
using SchulCloud.Database.Stores;
using SchulCloud.ServiceDefaults;
using SchulCloud.Web.Components;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Identity;
using SchulCloud.Web.Identity.EmailSenders;
using SchulCloud.Web.Identity.Managers;
using SchulCloud.Web.Services;
using SchulCloud.Web.Utils;
using SchulCloud.Web.Utils.Interfaces;

namespace SchulCloud.Web;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddValidatorsFromAssemblyContaining<IWeb>();
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

        IdentityBuilder identityBuilder = builder.Services.AddIdentity<User, Role>()
            .AddUserStore<SchulCloudUserStore>()
            .AddEntityFrameworkStores<SchulCloudDbContext>()
            .AddUserManager<SchulCloudUserManager>()
            .AddSignInManager<SchulCloudSignInManager>()
            .AddErrorDescriber<LocalizedErrorDescriber>()
            .AddEmailSender<MailKitEmailSender>()
            .AddPasswordResetLimiter<CachedRequestLimiter<User>>()
            .AddDefaultTokenProviders()
            .AddTokenProviders();

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
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services
            .AddBlazorBootstrap()
            .AddBlazoredLocalStorage()
            .AddScoped<ICookieHelper, CookieHelper>();

        WebApplication app = builder.Build();
        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
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
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        app.Run();
    }
}
