using Microsoft.ApplicationInsights.Channel;

namespace Super.AppInsights.Activities.Tests;

/// <summary>Captures telemetry in memory instead of sending it over the network.</summary>
public sealed class StubTelemetryChannel : ITelemetryChannel
{
    public List<ITelemetry> Sent { get; } = new();

    public bool? DeveloperMode { get; set; }

    public string EndpointAddress { get; set; } = string.Empty;

    public void Send(ITelemetry item) => Sent.Add(item);

    public void Flush() { }

    public void Dispose() { }
}
