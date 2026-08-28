using System.Activities;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Super.Extensions;

namespace Super.AppInsights.Activities;

/// <summary>
/// Sends an Application Insights custom event (named per <see cref="AppInsightsSettings.EventName"/>,
/// "Activity" by default) for every activity that completes. Applies to all activities.
/// </summary>
public sealed class AppInsightsActivityExtension : IActivityExtension, IDisposable
{
    private readonly AppInsightsSettings _settings;
    private readonly TelemetryConfiguration? _configuration;
    private readonly bool _ownsConfiguration;
    private readonly TelemetryClient? _client;

    /// <summary>
    /// Parameterless ctor for auto-discovery: reads the instrumentation key from the ambient project
    /// settings (<see cref="SuperRuntime.ProjectSettings"/>). If no key is configured, the extension is
    /// dormant (registers but emits nothing) rather than throwing.
    /// </summary>
    public AppInsightsActivityExtension()
        : this(ResolveAmbientSettings(), configuration: null, throwIfNoKey: false)
    {
    }

    public AppInsightsActivityExtension(AppInsightsSettings settings, TelemetryConfiguration? configuration = null)
        : this(settings, configuration, throwIfNoKey: false)
    {
    }

    private AppInsightsActivityExtension(AppInsightsSettings settings, TelemetryConfiguration? configuration, bool throwIfNoKey)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (configuration is not null)
        {
            _configuration = configuration;
            _ownsConfiguration = false;
            _client = new TelemetryClient(_configuration);
            return;
        }

        var key = settings.InstrumentationKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            if (throwIfNoKey)
            {
                throw new InvalidOperationException("No Application Insights instrumentation key configured.");
            }
            SuperRuntime.Log("App Insights extension is DORMANT - no instrumentation key in Project Settings.", System.Diagnostics.TraceEventType.Warning);
            return;
        }

        // Accept either a bare instrumentation key (GUID) or a full connection string.
        var connectionString = key.Contains('=', StringComparison.Ordinal) ? key : $"InstrumentationKey={key}";
        _configuration = new TelemetryConfiguration { ConnectionString = connectionString };
        _configuration.TelemetryChannel ??= new InMemoryChannel();
        _ownsConfiguration = true;
        _client = new TelemetryClient(_configuration);
    }

    private static AppInsightsSettings ResolveAmbientSettings()
        => SuperRuntime.ProjectSettings is { } reader
            ? AppInsightsSettings.FromProjectSettings(reader)
            : new AppInsightsSettings();

    public string Name => "AppInsights";

    public Type DeclaringType => typeof(AppInsightsActivityExtension);

    public string XmlNamespace => "clr-namespace:Super.AppInsights.Activities;assembly=Super.AppInsights.Activities";

    public bool AppliesTo(Activity activity) => true;

    public IReadOnlyList<ExtensionPropertyDefinition> Properties { get; } = Array.Empty<ExtensionPropertyDefinition>();

    public bool CapturesArguments => _settings.CaptureArguments;

    public void OnBeforeExecute(ExtensionExecutionContext context)
    {
        // One event per activity run, emitted on completion so the final state is included.
    }

    public void OnAfterExecute(ExtensionExecutionContext context)
    {
        if (_client is null || !_settings.Enabled)
        {
            return; // dormant (no instrumentation key, or telemetry disabled)
        }

        var properties = new Dictionary<string, string>
        {
            ["ActivityName"] = context.Activity.Name,
            ["ActivityId"] = context.Activity.Id,
            ["ActivityType"] = context.Activity.TypeName,
            ["ActivityInstanceId"] = context.Activity.InstanceId,
            ["State"] = context.State,
            ["WorkflowInstanceId"] = context.WorkflowInstanceId.ToString(),
        };

        if (_settings.CaptureArguments)
        {
            foreach (var argument in context.Arguments)
            {
                properties["Arg." + argument.Key] = argument.Value?.ToString() ?? string.Empty;
            }
        }

        _client.TrackEvent(_settings.EventName, properties);

        // The root activity (Id "1") closing means the run is ending. Flush() only hands telemetry to an
        // async sender, so on short-lived runs the process tears down before it transmits. FlushAsync
        // blocks until the batch is actually sent (or the timeout elapses), guaranteeing delivery.
        if (context.Activity.Id == "1")
        {
            FlushAndWait();
        }
    }

    /// <summary>
    /// Sends a custom event with an explicit name and caller-supplied properties. Backs the
    /// <see cref="TrackEvent"/> activity. The event name falls back to <see cref="AppInsightsSettings.EventName"/>
    /// when empty. No-op when dormant (no instrumentation key) or disabled.
    /// </summary>
    public void TrackCustomEvent(string? eventName, IDictionary<string, object>? properties)
    {
        if (_client is null || !_settings.Enabled)
        {
            return; // dormant (no instrumentation key, or telemetry disabled)
        }

        var name = string.IsNullOrWhiteSpace(eventName) ? _settings.EventName : eventName;
        _client.TrackEvent(name, SerializeProperties(properties));
    }

    /// <summary>
    /// Converts caller-supplied property values to the string form App Insights stores. Strings and value
    /// types use their (invariant-culture) string form; other reference types are JSON-serialized.
    /// </summary>
    internal static Dictionary<string, string>? SerializeProperties(IDictionary<string, object>? properties)
    {
        if (properties is null || properties.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, string>(properties.Count);
        foreach (var pair in properties)
        {
            result[pair.Key] = StringifyValue(pair.Value);
        }
        return result;
    }

    private static string StringifyValue(object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case string s:
                return s;
        }

        var type = value.GetType();
        if (type.IsValueType)
        {
            // int, bool, DateTime, Guid, enum, decimal, ... - their own string form is the most useful.
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        try
        {
            // Pass the runtime type so the actual object graph is serialized, not the static `object`.
            return JsonSerializer.Serialize(value, type);
        }
        catch (Exception ex)
        {
            SuperRuntime.Log($"Track Event: could not JSON-serialize value of type {type.FullName}: {ex.Message}", System.Diagnostics.TraceEventType.Warning);
            return value.ToString() ?? string.Empty;
        }
    }

    private void FlushAndWait()
    {
        if (_client is null)
        {
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            if (!_client.FlushAsync(cts.Token).GetAwaiter().GetResult())
            {
                SuperRuntime.Log("App Insights could not transmit telemetry before the timeout.", System.Diagnostics.TraceEventType.Warning);
            }
        }
        catch (Exception ex)
        {
            SuperRuntime.Log($"App Insights flush failed: {ex.Message}", System.Diagnostics.TraceEventType.Warning);
        }
    }

    /// <summary>Flushes buffered telemetry. Call before a short-lived process exits.</summary>
    public void Flush() => _client?.Flush();

    public void Dispose()
    {
        _client?.Flush();
        if (_ownsConfiguration)
        {
            _configuration?.Dispose();
        }
    }
}
