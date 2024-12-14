using Microsoft.Extensions.Options;
using SchulCloud.DbManager.Options;
using SchulCloud.ServiceDefaults.Services;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SchulCloud.DbManager.Cleaning;

internal class DbCleaner : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<CleanerOptions> _optionsMonitor;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<int> _executions;
    private readonly Histogram<double> _executionTimes;
    private readonly Counter<int> _objectsRemoved;

    public const string ActivitySourceName = nameof(DbCleaner);
    public const string MeterName = "dbCleaner";

    private CleanerOptions CleanerOptions => _optionsMonitor.CurrentValue;

    public DbCleaner(IServiceScopeFactory scopeFactory, ILogger<DbCleaner> logger, IMeterFactory meterFactory, IOptionsMonitor<CleanerOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _optionsMonitor = options;

        _activitySource = new(ActivitySourceName);

        _meter = meterFactory.Create(MeterName, version: "1.0.0");
        _executions = _meter.CreateCounter<int>($"{MeterName}.executions", description: "The amount of clean cycles executed during this runtime.", unit: "Executions");
        _executionTimes = _meter.CreateHistogram<double>($"{MeterName}.executionTimes", description: "The time it took for a clean cycle to execute.", unit: "Milliseconds");
        _objectsRemoved = _meter.CreateCounter<int>($"{MeterName}.removedObjects", description: "The total count of obsolete items removed.", unit: "Items");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanCycle(stoppingToken).ConfigureAwait(false);
            await Task.Delay(CleanerOptions.Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> RunCleanCycle(CancellationToken ct, bool automated = true)
    {
        using Activity? activity = _activitySource.StartActivity("Clean cycle triggered", automated ? ActivityKind.Internal : ActivityKind.Server);
        if (!_semaphore.Wait(0))     // Just checking whether its blocked.
        {
            _logger.LogInformation("Cleaner currently blocked. Waiting...");
            if (!await _semaphore.WaitAsync(CleanerOptions.Timeout, ct))
            {
                _logger.LogError("Cleanup cycle cancelled caused by exceeding timeout while waiting for execution.");
                return false;
            }
        }

        if (automated)
        {
            _logger.LogInformation("Started executing automated clean cycle.");
        }
        else
        {
            _logger.LogInformation("Started executing manually triggered clean cycle.");
        }

        int removedCount = 0;
        Stopwatch sw = new();
        try
        {
            CancellationTokenSource cancelCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cancelCts.CancelAfter(CleanerOptions.Timeout);

            using IServiceScope provider = _scopeFactory.CreateScope();
            IDataManager manager = provider.ServiceProvider.GetRequiredService<IDataManager>();

            sw.Start();
            removedCount += await manager.RemoveObsoleteAPIKeysAsync(ct).ConfigureAwait(false);
            removedCount += await manager.RemoveOldLoginAttemptsAsync(CleanerOptions.MaxLoginAttemptsPerUser, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during a cleanup cycle.");
        }
        finally
        {
            sw.Stop();
            _semaphore.Release();
        }

        _logger.LogInformation("Clean cycle finished. {removedCount} objects removed.", removedCount);

        string cleanedTag = removedCount switch
        {
            0 => "0",
            < 10 => "< 10",
            < 25 => "< 25",
            < 50 => "< 50",
            < 100 => "< 100",
            < 1000 => "< 1000",
            _ => "> 1000"
        };
        TagList tags = new()
        {
            { "Trigger", automated ? "Automated" : "Manual" },
            { "Cleaned items", cleanedTag }
        };

        _executions.Add(1, tags);
        _executionTimes.Record(sw.Elapsed.TotalMilliseconds, tags);
        _objectsRemoved.Add(removedCount);
        return true;
    }

    public override void Dispose()
    {
        base.Dispose();

        _semaphore.Dispose();
        _meter.Dispose();
    }
}
