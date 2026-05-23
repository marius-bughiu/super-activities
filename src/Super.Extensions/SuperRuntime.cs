using System.Diagnostics;
using UiPath.Activities.Api.Base;
using UiPath.Robot.Activities.Api;

namespace Super.Extensions;

/// <summary>
/// Ambient runtime state shared with extensions. <see cref="SuperInitialize"/> populates this from the
/// Robot host so extensions (e.g. App Insights) can read configuration and write diagnostics without a
/// dependency on the activity context.
/// </summary>
public static class SuperRuntime
{
    /// <summary>UiPath Project Settings reader (host-provided), or null when self-hosted.</summary>
    public static IActivitiesSettingsReader? ProjectSettings { get; set; }

    /// <summary>The Robot executor runtime (host-provided), used for diagnostics logging.</summary>
    public static IExecutorRuntime? Host { get; set; }

    /// <summary>Writes a diagnostic line to the Robot/Studio log (visible in Output / Orchestrator).</summary>
    public static void Log(string message, TraceEventType level = TraceEventType.Information)
    {
        try
        {
            Host?.LogMessage(new LogMessage { EventType = level, Message = "[SuperExtensions] " + message });
        }
        catch
        {
            // Diagnostics must never break a workflow.
        }
    }
}
