using System.Activities;
using System.Activities.Statements;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Super.Activities;
using Super.Extensions;
using Xunit;

namespace Super.AppInsights.Activities.Tests;

public sealed class AppInsightsExtensionTests : IDisposable
{
    public AppInsightsExtensionTests() => SuperExtensions.Clear();

    public void Dispose() => SuperExtensions.Clear();

    private static TelemetryConfiguration CreateConfig(StubTelemetryChannel channel)
        => new()
        {
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
            TelemetryChannel = channel,
        };

    private static void Run(Activity root)
    {
        var invoker = new WorkflowInvoker(root);
        invoker.UseSuperExtensions(root);
        invoker.Invoke();
    }

    [Fact]
    public void SendsActivityEvent_PerActivity_WithProperties()
    {
        var channel = new StubTelemetryChannel();
        AppInsights.Register(new AppInsightsSettings { InstrumentationKey = "x" }, CreateConfig(channel));

        var greet = new WriteMessage { DisplayName = "Greet", Message = new InArgument<string>("hi") };
        Run(new Sequence { Activities = { greet } });

        var events = channel.Sent.OfType<EventTelemetry>().Where(e => e.Name == "Activity").ToList();
        Assert.NotEmpty(events);

        var greetEvent = Assert.Single(events, e => e.Properties.TryGetValue("ActivityName", out var n) && n == "Greet");
        Assert.Equal("Closed", greetEvent.Properties["State"]);
        Assert.Equal(typeof(WriteMessage).FullName, greetEvent.Properties["ActivityType"]);
        Assert.True(greetEvent.Properties.ContainsKey("WorkflowInstanceId"));
    }

    [Fact]
    public void IncludesArguments_WhenCaptureArgumentsEnabled()
    {
        var channel = new StubTelemetryChannel();
        AppInsights.Register(new AppInsightsSettings { InstrumentationKey = "x", CaptureArguments = true }, CreateConfig(channel));

        var greet = new WriteMessage { DisplayName = "Greet", Message = new InArgument<string>("hi") };
        var root = new Sequence { Activities = { greet } };
        var invoker = new WorkflowInvoker(root);
        invoker.UseSuperExtensions(root, captureArguments: true);
        invoker.Invoke();

        var greetEvent = channel.Sent.OfType<EventTelemetry>()
            .First(e => e.Name == "Activity" && e.Properties.TryGetValue("ActivityName", out var n) && n == "Greet");
        Assert.Contains("Arg.Message", greetEvent.Properties.Keys);
    }

    [Fact]
    public void RespectsCustomEventName()
    {
        var channel = new StubTelemetryChannel();
        AppInsights.Register(
            new AppInsightsSettings { InstrumentationKey = "x", EventName = "WorkflowActivity" },
            CreateConfig(channel));

        Run(new Sequence { Activities = { new WriteMessage { DisplayName = "A", Message = new InArgument<string>("a") } } });

        Assert.Contains(channel.Sent.OfType<EventTelemetry>(), e => e.Name == "WorkflowActivity");
    }

    [Fact]
    public void SendsNothing_WhenDisabled()
    {
        var channel = new StubTelemetryChannel();
        AppInsights.Register(
            new AppInsightsSettings { InstrumentationKey = "x", Enabled = false },
            CreateConfig(channel));

        Run(new Sequence { Activities = { new WriteMessage { DisplayName = "A", Message = new InArgument<string>("a") } } });

        Assert.Empty(channel.Sent.OfType<EventTelemetry>());
    }

    [Fact]
    public void Dormant_WhenNoKeyConfigured()
    {
        // No key -> the extension constructs (and flushes) without throwing, so auto-discovery is safe.
        var exception = Record.Exception(() =>
        {
            using var ext = new AppInsightsActivityExtension(new AppInsightsSettings());
            ext.Flush();
        });
        Assert.Null(exception);
    }
}
