using Blazored.LocalStorage;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Server.Components;
using SchulCloud.Server.Options;
using SchulCloud.Server.Utils;
using SchulCloud.Server.Utils.Interfaces;

namespace SchulCloud.Server;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddOptions<LocalizationOptions>()
            .BindConfiguration("Localization");
        builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddOptions<PresentationOptions>()
            .BindConfiguration("Presentation");

        builder.Services.AddBlazorBootstrap();
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddScoped<IRequestState, RequestState>();
        builder.Services.AddScoped<ICookieHelper, CookieHelper>();

        WebApplication app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
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
        app.UseAntiforgery();

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
