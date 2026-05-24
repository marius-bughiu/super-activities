namespace Super.AppInsights.Activities;

/// <summary>
/// Stable keys for the App Insights project settings. The same keys are used to register the
/// settings at design time and to read their values at runtime.
/// </summary>
public static class AppInsightsSettingKeys
{
    public const string Category = "Super.AppInsights";

    /// <summary>
    /// The instrumentation key. UiPath project settings already track separate debug and runtime
    /// values per key, so this is a single setting; the runtime reader returns the value for the
    /// current execution mode.
    /// </summary>
    public const string InstrumentationKey = "Super.AppInsights.InstrumentationKey";

    /// <summary>Whether to include each activity's argument values in the telemetry events.</summary>
    public const string CaptureArguments = "Super.AppInsights.CaptureArguments";

    /// <summary>Whether telemetry is sent at all. Defaults to <c>true</c>.</summary>
    public const string Enabled = "Super.AppInsights.Enabled";

    /// <summary>Name of the custom event emitted per activity. Defaults to "Activity".</summary>
    public const string EventName = "Super.AppInsights.EventName";
}
