using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Settings;

namespace Super.AppInsights.Activities;

/// <summary>
/// Discovered by UiPath Studio, which calls <see cref="Initialize"/>. Adds an "App Insights" category
/// to Project Settings with the debug and runtime instrumentation key fields.
/// </summary>
public sealed class AppInsightsDesignRegistration : IRegisterWorkflowDesignApi
{
    public void Initialize(IWorkflowDesignApi api) => Register(api.Settings);

    internal static void Register(IActivitiesSettingsService settings)
    {
        var category = new SettingsCategory
        {
            Key = AppInsightsSettingKeys.Category,
            Header = "App Insights",
            Description = "Application Insights telemetry for activities.",
        };
        settings.AddCategory(category);

        settings.AddSetting(category, new SingleValueEditorDescription<string>
        {
            Key = AppInsightsSettingKeys.InstrumentationKey,
            Label = "Instrumentation Key",
            Description = "Application Insights instrumentation key (or full connection string).",
            DefaultValue = string.Empty,
        });

        settings.AddSetting(category, new SingleValueEditorDescription<bool>
        {
            Key = AppInsightsSettingKeys.CaptureArguments,
            Label = "Capture arguments",
            Description = "Include each activity's argument values in the telemetry events (more overhead).",
            DefaultValue = false,
        });
    }
}
