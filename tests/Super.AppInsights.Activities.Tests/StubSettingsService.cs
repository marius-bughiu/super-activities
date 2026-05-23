using UiPath.Studio.Activities.Api.Settings;

namespace Super.AppInsights.Activities.Tests;

/// <summary>Captures what an IRegisterWorkflowDesignApi implementation registers.</summary>
public sealed class StubSettingsService : IActivitiesSettingsService
{
    public List<SettingsCategory> Categories { get; } = new();
    public List<SettingDescriptionBase> Settings { get; } = new();

    public void AddCategory(SettingsCategory category) => Categories.Add(category);

    public void AddSection(SettingsCategory category, SettingsSection section) { }

    public void AddSetting(SettingsEditorControlContainer parent, SettingDescriptionBase setting) => Settings.Add(setting);

    public void AddSetting(SettingsEditorControlContainer parent, SettingsEditorControl setting) { }

    public bool TrySetValue(string key, string value) => true;

    public Task<bool> TrySetValueAsync(string key, string value) => Task.FromResult(true);

    public IDisposable RegisterValueChangedObserver(SettingsObserver observer) => new Noop();

    public bool TryGetValue<T>(string key, out T value)
    {
        value = default!;
        return false;
    }

    private sealed class Noop : IDisposable
    {
        public void Dispose() { }
    }
}
