using Microsoft.ApplicationInsights.Extensibility;
using Super.Extensions;
using UiPath.Activities.Api.Base;

namespace Super.AppInsights.Activities;

/// <summary>Entry point for wiring up Application Insights activity telemetry.</summary>
public static class AppInsights
{
    /// <summary>
    /// Creates and registers the App Insights activity extension. Returns the instance so the caller
    /// can <see cref="AppInsightsActivityExtension.Flush"/> / dispose it (important for short-lived runs).
    /// </summary>
    public static AppInsightsActivityExtension Register(AppInsightsSettings settings, TelemetryConfiguration? configuration = null)
    {
        var extension = new AppInsightsActivityExtension(settings, configuration);
        SuperExtensions.Register(extension);
        return extension;
    }

    /// <summary>
    /// Registers the extension using the instrumentation key configured in UiPath Project Settings,
    /// read through the host-provided <see cref="IActivitiesSettingsReader"/>.
    /// </summary>
    public static AppInsightsActivityExtension RegisterFromProjectSettings(
        IActivitiesSettingsReader reader,
        TelemetryConfiguration? configuration = null)
        => Register(AppInsightsSettings.FromProjectSettings(reader), configuration);
}
