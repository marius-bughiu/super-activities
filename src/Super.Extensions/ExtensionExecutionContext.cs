using System.Activities;
using System.Activities.Tracking;

namespace Super.Extensions;

/// <summary>
/// Passed to <see cref="IActivityExtension.OnBeforeExecute"/> / <see cref="IActivityExtension.OnAfterExecute"/>.
/// Scoped to a single extension and a single activity-execution event, so <see cref="GetProperty{T}"/>
/// reads from the invoking extension's own configured values.
/// </summary>
public sealed class ExtensionExecutionContext
{
    private readonly Activity _activity;
    private readonly IActivityExtension _extension;

    internal ExtensionExecutionContext(
        ActivityInfo activity,
        string state,
        Guid workflowInstanceId,
        Activity activityDefinition,
        IActivityExtension extension,
        IReadOnlyDictionary<string, object?> arguments)
    {
        Activity = activity;
        State = state;
        WorkflowInstanceId = workflowInstanceId;
        _activity = activityDefinition;
        _extension = extension;
        Arguments = arguments;
    }

    /// <summary>Identity of the activity (Name / Id / TypeName / InstanceId).</summary>
    public ActivityInfo Activity { get; }

    /// <summary>"Executing", "Closed", "Faulted", or "Canceled".</summary>
    public string State { get; }

    public Guid WorkflowInstanceId { get; }

    /// <summary>Captured argument values when a capture profile is enabled; otherwise empty.</summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>Reads a configured value of one of THIS extension's properties.</summary>
    public T? GetProperty<T>(string name)
    {
        if (ActivityExtensionData.TryGet(_activity, _extension, name, out var value) && value is not null)
        {
            if (value is T typed)
            {
                return typed;
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }

        var def = FindDefinition(name);
        if (def?.DefaultValue is T typedDefault)
        {
            return typedDefault;
        }
        return default;
    }

    private ExtensionPropertyDefinition? FindDefinition(string name)
    {
        foreach (var def in _extension.Properties)
        {
            if (def.Name == name)
            {
                return def;
            }
        }
        return null;
    }
}
