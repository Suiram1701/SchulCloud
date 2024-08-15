using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using SchulCloud.Web.Options;

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
            .Configure<PasswordResetOptions>(builder.Configuration.GetSection("Identity:PasswordReset"))
            .Configure<EmailSenderOptions>(builder.Configuration.GetSection("Identity:EmailSender"))
            .Configure<DataProtectionTokenProviderOptions>(builder.Configuration.GetSection($"Identity:TokenProviders:DataProtectionTokenProvider"));

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
        builder.Services.Configure<RequestLimiterOptions>(builder.Configuration.GetSection("RequestLimiter"));

        return builder;
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
