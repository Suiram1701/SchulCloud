using Microsoft.EntityFrameworkCore;
using SchulCloud.Database;

namespace SchulCloud.DbManager.Extensions;

public static class WebApplicationsExtensions
{
    public static WebApplication MapCommands(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/commands/reset-db", async context =>
        {
            DbContext dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
            await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
        });
        app.MapGet("/commands/drop-db", async context =>
        {
            DbContext dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);

            _ = app.StopAsync();
        });
        return app;
    }
}
