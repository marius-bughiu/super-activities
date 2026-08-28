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
    public void TrackEvent_SendsNamedEvent_WithSerializedProperties()
    {
        var channel = new StubTelemetryChannel();
        AppInsights.Register(new AppInsightsSettings { InstrumentationKey = "x" }, CreateConfig(channel));

        var props = new Dictionary<string, object>
        {
            ["OrderId"] = 42,
            ["Customer"] = "Acme",
            ["Lines"] = new[] { 1, 2, 3 },
        };
        var activity = new TrackEvent
        {
            EventName = new InArgument<string>("OrderPlaced"),
            // A Dictionary can't be a Literal, so supply it via an expression (as Studio does at runtime).
            Properties = new InArgument<Dictionary<string, object>>(_ => props),
        };

        Run(new Sequence { Activities = { activity } });

        var evt = Assert.Single(channel.Sent.OfType<EventTelemetry>(), e => e.Name == "OrderPlaced");
        Assert.Equal("42", evt.Properties["OrderId"]);
        Assert.Equal("Acme", evt.Properties["Customer"]);
        Assert.Equal("[1,2,3]", evt.Properties["Lines"]);
    }

    [Fact]
    public void TrackEvent_SendsNothing_WhenDisabled()
    {
        var channel = new StubTelemetryChannel();
        AppInsights.Register(new AppInsightsSettings { InstrumentationKey = "x", Enabled = false }, CreateConfig(channel));

        var activity = new TrackEvent { EventName = new InArgument<string>("OrderPlaced") };
        Run(new Sequence { Activities = { activity } });

        Assert.DoesNotContain(channel.Sent.OfType<EventTelemetry>(), e => e.Name == "OrderPlaced");
    }

    [Fact]
    public void SerializeProperties_StringifiesByValueKind()
    {
        var props = new Dictionary<string, object>
        {
            ["str"] = "hello",
            ["num"] = 42,
            ["flag"] = true,
            ["obj"] = new { Id = 7, Name = "x" },
            ["list"] = new[] { 1, 2, 3 },
            ["nullVal"] = null!,
        };

        var result = AppInsightsActivityExtension.SerializeProperties(props)!;

        Assert.Equal("hello", result["str"]);          // string passes through as-is
        Assert.Equal("42", result["num"]);             // value type -> ToString
        Assert.Equal("True", result["flag"]);          // value type -> ToString
        Assert.Equal("{\"Id\":7,\"Name\":\"x\"}", result["obj"]); // reference type -> JSON
        Assert.Equal("[1,2,3]", result["list"]);       // reference type -> JSON
        Assert.Equal(string.Empty, result["nullVal"]); // null -> empty
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
