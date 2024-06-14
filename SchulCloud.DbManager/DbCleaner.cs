
namespace SchulCloud.DbManager;

internal class DbCleaner(IServiceProvider services, ILogger<DbCleaner> logger) : BackgroundService
{
    private readonly IServiceProvider _services = services;
    private readonly ILogger _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    }
}
