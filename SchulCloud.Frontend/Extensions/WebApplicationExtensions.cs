using Microsoft.Extensions.Options;
using SchulCloud.Frontend.Options;
using SchulCloud.ServiceDefaults.Options;

namespace SchulCloud.Frontend.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseFaviconRedirect(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ServiceOptions appOptions = app.Services.GetRequiredService<IOptions<ServiceOptions>>().Value;

        app.MapGet(appOptions.GetPathWithBase("favicon.ico"), (IOptionsSnapshot<PresentationOptions> optionsAccessor) =>
        {
            string? iconPath = optionsAccessor.Value.GetBestFavicon()?.Path;
            if (!string.IsNullOrEmpty(iconPath))
            {
                return Results.Redirect(iconPath, permanent: true);
            }
            else
            {
                return Results.NotFound();
            }
        });

        return app;
    }
}
