using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SchulCloud.Frontend.CircuitHandlers;

internal class TelemetryHandler : CircuitHandler
{
    private readonly ILogger _logger;
    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    private readonly Meter _meter;
    private readonly UpDownCounter<int> _totalCircuits;
    private readonly UpDownCounter<int> _totalConnections;

    private readonly ConcurrentDictionary<string, Activity> _circuitActivities = new();

    public const string ActivitySourceName = "Circuits.TelemetryHandler";
    public const string MeterName = "Microsoft.AspNetCore.Components.Server.Circuits";

    public TelemetryHandler(ILogger<TelemetryHandler> logger, IMeterFactory meterFactory)
    {
        _logger = logger;

        _meter = meterFactory.Create(MeterName);
        _totalCircuits = _meter.CreateUpDownCounter<int>("circuits.amount", description: "The amount of circuits hosted on the server. A circuit stores a clients state on the server but doesn't require a connection to exist.", unit: "Circuits");
        _totalConnections = _meter.CreateUpDownCounter<int>("circuits.connectedClients", description: "The amount of clients connected to the server. A connection requires also an circuit.", unit: "Connections");
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Activity? circuitActivity = _activitySource.StartActivity("Circuit lifetime", ActivityKind.Server)?.AddTag("CircuitId", circuit.Id);
        if (circuitActivity is not null)
        {
            _circuitActivities[circuit.Id] = circuitActivity;
        }
        _logger.LogInformation("Circuit '{circuitId}' opened", circuit.Id);
        _totalCircuits.Add(1);

        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit '{circuitId}' closed", circuit.Id);
        if (_circuitActivities.Remove(circuit.Id, out Activity? circuitActivity))
        {
            circuitActivity.Dispose();
        }
        _totalCircuits.Add(-1);

        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _circuitActivities.GetValueOrDefault(circuit.Id)?.AddEvent(new("Connected"));
        _totalConnections.Add(1);

        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _circuitActivities.GetValueOrDefault(circuit.Id)?.AddEvent(new("Disconnected"));
        _totalConnections.Add(-1);

        return Task.CompletedTask;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next) => async context =>
    {
        Activity? circuitActivity = _circuitActivities.GetValueOrDefault(context.Circuit.Id)?.AddEvent(new("Inbound activity"));
        using Activity? inboundActivity = _activitySource.StartActivity("Inbound activity", ActivityKind.Server, circuitActivity?.Context ?? default)?.AddTag("CircuitId", context.Circuit.Id);
        if (inboundActivity is not null)
        {
            circuitActivity?.AddLink(new(inboundActivity.Context));
        }

        await next(context);
    };
}
