using System.Activities;
using System.ComponentModel;
using Super.Extensions;

namespace Super.AppInsights.Activities;

/// <summary>
/// Sends a custom Application Insights event with the given name and properties, through the App Insights
/// extension registered by <c>Super Initialize</c>. Reference-type property values are JSON-serialized;
/// strings and value types use their string form. No-op when telemetry is dormant or disabled.
/// </summary>
[DisplayName("Track Event")]
[Description("Sends a custom Application Insights event with the given name and properties.")]
public sealed class TrackEvent : CodeActivity
{
    public TrackEvent()
    {
        DisplayName = "Track Event";
    }

    /// <summary>Name of the custom event. Falls back to the configured event name when empty.</summary>
    [RequiredArgument]
    public InArgument<string> EventName { get; set; } = new();

    /// <summary>Custom properties attached to the event. Reference-type values are JSON-serialized.</summary>
    public InArgument<Dictionary<string, object>> Properties { get; set; } = new();

    protected override void Execute(CodeActivityContext context)
    {
        var extension = SuperExtensions.All.OfType<AppInsightsActivityExtension>().FirstOrDefault();
        if (extension is null)
        {
            SuperRuntime.Log(
                "Track Event: App Insights extension is not registered. Add 'Super Initialize' at the start of your workflow.",
                System.Diagnostics.TraceEventType.Warning);
            return;
        }

        extension.TrackCustomEvent(context.GetValue(EventName), context.GetValue(Properties));
    }
}
