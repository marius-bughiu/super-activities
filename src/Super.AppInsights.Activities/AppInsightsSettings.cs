using UiPath.Activities.Api.Base;

namespace Super.AppInsights.Activities;

/// <summary>
/// App Insights configuration. The instrumentation key is a single value; UiPath project settings
/// already track separate debug/runtime values for it, and the runtime reader returns the value for
/// the current execution mode.
/// </summary>
public sealed class AppInsightsSettings
{
    /// <summary>Application Insights instrumentation key.</summary>
    public string? InstrumentationKey { get; set; }

    /// <summary>Include each activity's argument values in the telemetry events.</summary>
    public bool CaptureArguments { get; set; }

    /// <summary>Name of the custom event emitted per activity. Defaults to "Activity".</summary>
    public string EventName { get; set; } = "Activity";

    /// <summary>
    /// Builds settings from the UiPath project settings reader (the value configured in Studio's
    /// Project Settings under the "App Insights" category). The reader is provided by the Robot host
    /// at runtime via <c>context.GetExtension&lt;IActivitiesSettingsReader&gt;()</c>.
    /// </summary>
    public static AppInsightsSettings FromProjectSettings(IActivitiesSettingsReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return new AppInsightsSettings
        {
            InstrumentationKey = reader.TryGetValue(AppInsightsSettingKeys.InstrumentationKey, out string value) ? value : null,
            CaptureArguments = ReadBool(reader, AppInsightsSettingKeys.CaptureArguments),
        };
    }

    private static bool ReadBool(IActivitiesSettingsReader reader, string key)
    {
        if (reader.TryGetValue(key, out bool b))
        {
            return b;
        }
        return reader.TryGetValue(key, out string s) && bool.TryParse(s, out var parsed) && parsed;
    }
}
