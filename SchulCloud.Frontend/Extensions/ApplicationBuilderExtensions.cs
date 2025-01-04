using Fido2NetLib;
using GoogleMapsComponents;
using Microsoft.AspNetCore.Localization;
using MudBlazor;
using MudBlazor.Services;
using SchulCloud.Frontend.JsInterop;
using SchulCloud.Frontend.Options;
using SchulCloud.Frontend.RequestCultureProviders;
using SchulCloud.Identity;

namespace SchulCloud.Frontend.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the every options from the configuration.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static void ConfigureOptions(this IHostApplicationBuilder builder)
    {
        // Visual presentation
        builder.Services
            .Configure<PresentationOptions>(builder.Configuration.GetSection("Presentation"))
            .Configure<RequestLocalizationOptions>(localizationOptions =>
            {
                // By default they contain the current culture/uiCulture.
                (localizationOptions.SupportedCultures ??= []).Clear();
                (localizationOptions.SupportedUICultures ??= []).Clear();

                localizationOptions.RequestCultureProviders = [
                    new ClaimCultureProvider(),
                    new CookieRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider()
                ];
            })
            .Configure<RequestLocalizationOptions>(builder.Configuration.GetSection("RequestLocalization"));

        // MudBlazor
        builder.Services
            .Configure<SnackbarConfiguration>(builder.Configuration.GetSection("MudBlazor:Snackbar"))
            .Configure<ResizeOptions>(builder.Configuration.GetSection("MudBlazor:Resize"))
            .Configure<ResizeObserverOptions>(builder.Configuration.GetSection("MudBlazor:ResizeObserver"))
            .Configure<PopoverOptions>(builder.Configuration.GetSection("MudBlazor:Popover"));

        // Other
        builder.Services
            .Configure<RequestLimiterOptions>(builder.Configuration.GetSection("RequestLimiter"))
            .Configure<Fido2Configuration>(builder.Configuration.GetSection("Fido2"))
            .Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
        builder.ConfigureIdentity();
    }

    public static IHostApplicationBuilder AddConfiguredGoogleMapsServices(this IHostApplicationBuilder builder)
    {
        string? mapsApiKey = builder.Configuration["GoogleMaps:ApiKey"];
        if (!string.IsNullOrWhiteSpace(mapsApiKey))
        {
            builder.Services.AddBlazorGoogleMaps(mapsApiKey);
        }

        return builder;
    }

    public static IServiceCollection AddFido2Services(this IServiceCollection services)
    {
        services
            .AddScoped<WebAuthnInterop>()
            .AddMemoryCache()
            .AddDistributedMemoryCache()
            .AddFido2(options =>
            {
                // Aspire stores the application url with port in this env variable.
                string? envVar = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
                if (string.IsNullOrWhiteSpace(envVar))
                {
                    return;
                }

                HashSet<string> origins = [.. options.Origins];
                foreach (string url in envVar.Split(';'))
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out _))
                    {
                        origins.Add(url);
                    }
                }

                options.Origins = origins;
            })
            .AddCachedMetadataService(metadataBuilder =>
            {
                metadataBuilder.AddFidoMetadataRepository();
            });

        return services;
    }
}
