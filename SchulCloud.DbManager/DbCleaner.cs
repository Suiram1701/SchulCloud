
namespace SchulCloud.DbManager;

internal class DbCleaner(IServiceProvider services, ILogger<DbCleaner> logger) : BackgroundService
{
    private readonly IServiceProvider _services = services;
    private readonly ILogger _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A placeholder that hold it in the execution state until it is requested to stop.
        TaskCompletionSource completionSource = new();
        stoppingToken.Register(completionSource.SetResult);
        await completionSource.Task;
    }
}
