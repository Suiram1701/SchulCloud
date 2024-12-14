using SchulCloud.Database;
using SchulCloud.DbManager.Cleaning;

namespace SchulCloud.DbManager.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapCommands(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/commands/cleanup", async (HttpContext context, DbCleaner cleaner) =>
        {
            return await cleaner.RunCleanCycle(context.RequestAborted, automated: false).ConfigureAwait(false)
                ? Results.NoContent()
                : Results.InternalServerError();
        });
        app.MapGet("/commands/reset-db", async (HttpContext context, AppDbContext dbContext) =>
        {
            await dbContext.Database.EnsureDeletedAsync(context.RequestAborted).ConfigureAwait(false);
            await dbContext.Database.EnsureCreatedAsync(context.RequestAborted).ConfigureAwait(false);
        });
        app.MapGet("/commands/drop-db", async (HttpContext context, AppDbContext dbContext) =>
        {
            await dbContext.Database.EnsureDeletedAsync(context.RequestAborted).ConfigureAwait(false);
            _ = app.StopAsync();
        });
        return app;
    }
}
