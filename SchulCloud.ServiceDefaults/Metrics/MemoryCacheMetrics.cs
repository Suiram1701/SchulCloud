using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SchulCloud.ServiceDefaults.Metrics;

public class MemoryCacheMetrics : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IMemoryCache? _cache;

    private readonly Timer _timer = default!;

    private readonly Meter _meter = default!;
    private readonly Gauge<long> _currentEntryCount = default!;
    private readonly Gauge<long> _currentEstimatedSize = default!;
    private readonly Gauge<long> _totalHits = default!;
    private readonly Gauge<long> _totalMisses = default!;

    private bool disposedValue;

    public const string Name = "SchulCloud.ServiceDefaults.Metrics.MemoryCacheMetrics";

    public MemoryCacheMetrics(ILogger<MemoryCacheMetrics> logger, IServiceProvider serviceProvider, IMeterFactory meterFactory)
    {
        _logger = logger;
        _cache = serviceProvider.GetService<IMemoryCache>();
        if (_cache is null)     // Is null if the cache isn't registered.
        {
            return;
        }

        _timer = new(TimeSpan.FromSeconds(10));
        _timer.Elapsed += Timer_Elapsed;

        _meter = meterFactory.Create(name: Name, version: "1.0.0");
        _currentEntryCount = _meter.CreateGauge<long>(name: $"{Name}.entries", description: "The count of all entries in the cache.", unit: "Entries");
        _currentEstimatedSize = _meter.CreateGauge<long>(name: $"{Name}.entries.size", description: "The estimated size of all cache entries.", unit: "bytes");
        _totalHits = _meter.CreateGauge<long>(name: $"{Name}.total.hits", description: "The total number of cache hits.", unit: "Hits");
        _totalMisses = _meter.CreateGauge<long>(name: $"{Name}.total.misses", description: "The total number of cache misses.", unit: "Misses");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            _timer.Start();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        return Task.CompletedTask;
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        _logger.LogTrace("Executing MemoryCache metrics");

        MemoryCacheStatistics? statistics = _cache!.GetCurrentStatistics();
        if (statistics is null)
        {
            _logger.LogInformation("Unable to execute MemoryCache metrics. Value is 'null'.");
        }
        else
        {
            _currentEntryCount.Record(statistics.CurrentEntryCount);
            if (statistics.CurrentEstimatedSize is not null)
            {
                _currentEstimatedSize.Record(statistics.CurrentEstimatedSize.Value);
            }
            _totalHits.Record(statistics.TotalHits);
            _totalMisses.Record(statistics.TotalMisses);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _meter?.Dispose();
            }
            disposedValue = true;
        }
    }

    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
