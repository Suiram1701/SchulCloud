using Microsoft.Extensions.Options;
using Quartz;
using SchulCloud.DbManager.Options;
using SchulCloud.DbManager.Quartz;
using SchulCloud.ServiceDefaults.Services;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SchulCloud.DbManager.Cleaning;

internal class CleanerJob : IJob, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<CleanerOptions> _optionsMonitor;

    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<int> _executions;
    private readonly Histogram<double> _executionTimes;
    private readonly Counter<int> _objectsRemoved;

    public const string ActivitySourceName = nameof(CleanerJob);
    public const string MeterName = "SchulCloud.DbManager.Cleaning.CleanerJob";

    private CleanerOptions CleanerOptions => _optionsMonitor.CurrentValue;

    public CleanerJob(IServiceScopeFactory scopeFactory, ILogger<CleanerJob> logger, IMeterFactory meterFactory, IOptionsMonitor<CleanerOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _optionsMonitor = options;

        _activitySource = new(ActivitySourceName);

        _meter = meterFactory.Create(MeterName, version: "1.0.0");
        _executions = _meter.CreateCounter<int>("cleanerJob.executions", description: "The amount of clean cycles executed during this runtime.", unit: "Executions");
        _executionTimes = _meter.CreateHistogram<double>("cleanerJob.executionTimes", description: "The time it took for a clean cycle to execute.", unit: "Milliseconds");
        _objectsRemoved = _meter.CreateCounter<int>("cleanerJob.removedObjects", description: "The total count of obsolete items removed.", unit: "Items");
    }

    public async Task Execute(IJobExecutionContext context)
    {
        ActivityContext? triggerActivity = context.MergedJobDataMap.Get("trigger") as ActivityContext?;
        if (triggerActivity is not null)
        {
            Activity.Current?.AddLink(new(triggerActivity.Value));
        }

        bool automated = context.Trigger.Key.Equals(Jobs.CleanerJobTimeTrigger);
        if (automated)
        {
            _logger.LogInformation("Started executing automated clean cycle...");
        }
        else
        {
            _logger.LogInformation("Started executing manually triggered clean cycle...");
        }

        CancellationTokenSource cancelCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cancelCts.CancelAfter(CleanerOptions.Timeout);

        using IServiceScope provider = _scopeFactory.CreateScope();
        IDataManager manager = provider.ServiceProvider.GetRequiredService<IDataManager>();

        int removedCount = 0;
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            removedCount += await manager.RemoveObsoleteAPIKeysAsync(context.CancellationToken).ConfigureAwait(false);
            removedCount += await manager.RemoveOldLoginAttemptsAsync(CleanerOptions.MaxLoginAttemptsPerUser, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during a cleanup cycle.");
        }
        finally
        {
            sw.Stop();
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
        _objectsRemoved.Add(removedCount, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
