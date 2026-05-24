using Xunit;

namespace Super.AppInsights.Activities.Tests;

public sealed class AppInsightsDesignRegistrationTests
{
    [Fact]
    public void RegistersCategoryWithAllSettings()
    {
        var settings = new StubSettingsService();

        AppInsightsDesignRegistration.Register(settings);

        var category = Assert.Single(settings.Categories);
        Assert.Equal(AppInsightsSettingKeys.Category, category.Key);
        Assert.Equal("App Insights", category.Header);

        Assert.Contains(settings.Settings, s => s.Key == AppInsightsSettingKeys.InstrumentationKey && s.Label == "Instrumentation Key");
        Assert.Contains(settings.Settings, s => s.Key == AppInsightsSettingKeys.Enabled && s.Label == "Enabled");
        Assert.Contains(settings.Settings, s => s.Key == AppInsightsSettingKeys.CaptureArguments && s.Label == "Capture arguments");
        Assert.Contains(settings.Settings, s => s.Key == AppInsightsSettingKeys.EventName && s.Label == "Event name");
    }
}
