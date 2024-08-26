using Fido2NetLib;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using SchulCloud.Store;
using SchulCloud.Web.Options;
using SchulCloud.Web.Services;

namespace SchulCloud.Web.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the every options from the configuration.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IHostApplicationBuilder ConfigureOptions(this IHostApplicationBuilder builder)
    {
        // Identity
        builder.Services
            .Configure<IdentityOptions>(builder.Configuration.GetSection("Identity"))
            .Configure<EmailSenderOptions>(builder.Configuration.GetSection("Identity:EmailSender"))
            .Configure<DataProtectionTokenProviderOptions>(builder.Configuration.GetSection("Identity:TokenProviders:DataProtectionTokenProvider"))
            .Configure<AuthenticationCodeProviderOptions>(builder.Configuration.GetSection("Identity:TokenProviders:AuthenticationCodeTokenProvider")); 

        // Visual presentation
        builder.Services
            .Configure<PresentationOptions>(builder.Configuration.GetSection("Presentation"))
            .Configure<RequestLocalizationOptions>(localizationOptions =>
            {
                // By default they contain the current culture/uiCulture.
                (localizationOptions.SupportedCultures ??= []).Clear();
                (localizationOptions.SupportedUICultures ??= []).Clear();

                localizationOptions.RequestCultureProviders = [
                    new CookieRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider()
                ];
            })
            .Configure<RequestLocalizationOptions>(builder.Configuration.GetSection("RequestLocalization"));

        // Other
        builder.Services
            .Configure<RequestLimiterOptions>(builder.Configuration.GetSection("RequestLimiter"))
            .Configure<Fido2Configuration>(builder.Configuration.GetSection("Fido2"));
        builder.ConfigureManagers();

        return builder;
    }

    public static IServiceCollection AddFido2Services(this IServiceCollection services)
    {
        services
            .AddScoped<WebAuthnService>()
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

                HashSet<string> origins = new(options.Origins);
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

    /// <summary>
    /// Adds the static files under /_static to the request pipeline
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder pipeline.</returns>
    public static IApplicationBuilder UseStaticFileServer(this IApplicationBuilder app)
    {
        return app.MapWhen(context => context.Request.Path.StartsWithSegments("/_static"), subApp =>
            {
                subApp
                    .UseStaticFiles("/_static")
                    .Use((HttpContext context, RequestDelegate next) =>
                    {
                        // The request is aborted here because /_static contains only static files and at this point it couldn't be a static file.
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return Task.CompletedTask;
                    });
            });
    }
}
