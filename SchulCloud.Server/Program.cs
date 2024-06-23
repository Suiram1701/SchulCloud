using Blazored.LocalStorage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Database;
using SchulCloud.Database.Models;
using SchulCloud.Server.Components;
using SchulCloud.Server.Options;
using SchulCloud.Server.Utils;
using SchulCloud.Server.Utils.Interfaces;
using SchulCloud.ServiceDefaults;

namespace SchulCloud.Server;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.AddNpgsqlDbContext<SchulCloudDbContext>("schulcloud-db");
        builder.Services.AddIdentity<User, Role>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
            .AddEntityFrameworkStores<SchulCloudDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/auth/signIn";
            options.LogoutPath = "/auth/signOut";
            options.AccessDeniedPath = "/error/403";

            options.ReturnUrlParameter = "returnUrl";

            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
        });

        builder.Services.Configure<PresentationOptions>(builder.Configuration.GetSection("Presentation"));
        builder.Services.Configure<LocalizationOptions>(builder.Configuration.GetSection("Localization"));
        builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services
            .AddBlazorBootstrap()
            .AddBlazoredLocalStorage()
            .AddScoped<IRequestState, RequestState>()
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

        app.UseHttpsRedirection();
        app.UseStatusCodePagesWithReExecute("/error/{0}");

        app.UseStaticFiles("/static");

        app.UseAuthentication();
        app.UseAntiforgery();
        app.UseAuthorization();

        app.UseRequestLocalization(options =>
        {
            LocalizationOptions localizationOptions = app.Services.GetRequiredService<IOptions<LocalizationOptions>>().Value;

            options.SetDefaultCulture("en");
            options.AddSupportedCultures(localizationOptions.SupportedCultures.ToArray());
            options.AddSupportedUICultures(localizationOptions.SupportedCultures.ToArray());

            options.FallBackToParentCultures = localizationOptions.FallbackToParentCulture;
            options.FallBackToParentUICultures = localizationOptions.FallbackToParentCulture;

            options.ApplyCurrentCultureToResponseHeaders = localizationOptions.ApplyToHeader;

            options.RequestCultureProviders = [
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
                ];
        });

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        app.Run();
    }
}