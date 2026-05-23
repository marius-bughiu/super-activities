using System.Activities;
using System.Activities.Tracking;
using System.Reflection;

namespace Super.Extensions;

/// <summary>
/// Reaches into the live CoreWF executor (via reflection over UiPath.Workflow internals) from inside a
/// running activity, and attaches a <see cref="TrackingParticipant"/> to the workflow instance's tracking
/// pipeline. This lets the framework instrument every subsequent activity even in hosts (e.g. the UiPath
/// Robot) that never call our public wire-up, because the Robot still runs on CoreWF.
///
/// Internals used (UiPath.Workflow 6.0.x): ActivityContext._executor -> ActivityExecutor._host ->
/// WorkflowInstance.{WorkflowDefinition, _trackingProvider, HasTrackingParticipant} and
/// TrackingProvider.AddParticipant. If a host runs with no tracking participant, we create the provider
/// and flip HasTrackingParticipant so records start flowing.
/// </summary>
internal static class LiveExecutionHook
{
    private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static Activity GetRootActivity(ActivityContext context)
    {
        var host = GetHost(context);
        var definition = GetPropertyUpHierarchy(host.GetType(), "WorkflowDefinition")?.GetValue(host) as Activity;
        return definition ?? throw new InvalidOperationException("Could not resolve WorkflowInstance.WorkflowDefinition via reflection.");
    }

    public static void AddTrackingParticipant(ActivityContext context, TrackingParticipant participant)
    {
        var host = GetHost(context);
        var hostType = host.GetType();

        var providerField = GetFieldUpHierarchy(hostType, "_trackingProvider")
            ?? throw new InvalidOperationException("WorkflowInstance._trackingProvider not found.");

        var provider = providerField.GetValue(host);
        if (provider is null)
        {
            // No tracking was configured by the host: create the provider and enable tracking.
            var root = GetRootActivity(context);
            provider = Activator.CreateInstance(providerField.FieldType, root)
                ?? throw new InvalidOperationException("Could not construct TrackingProvider.");
            providerField.SetValue(host, provider);

            var hasParticipant = GetPropertyUpHierarchy(hostType, "HasTrackingParticipant");
            hasParticipant?.GetSetMethod(nonPublic: true)?.Invoke(host, new object[] { true });
        }

        var addParticipant = provider.GetType().GetMethod("AddParticipant", InstanceAny)
            ?? throw new InvalidOperationException("TrackingProvider.AddParticipant not found.");
        addParticipant.Invoke(provider, new object[] { participant });
    }

    private static object GetHost(ActivityContext context)
    {
        var executorField = typeof(ActivityContext).GetField("_executor", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ActivityContext._executor not found.");
        var executor = executorField.GetValue(context)
            ?? throw new InvalidOperationException("ActivityContext._executor was null.");

        var hostField = GetFieldUpHierarchy(executor.GetType(), "_host")
            ?? throw new InvalidOperationException("ActivityExecutor._host not found.");
        return hostField.GetValue(executor)
            ?? throw new InvalidOperationException("ActivityExecutor._host was null.");
    }

    private static FieldInfo? GetFieldUpHierarchy(Type? type, string name)
    {
        for (; type is not null; type = type.BaseType)
        {
            var field = type.GetField(name, InstanceAny | BindingFlags.DeclaredOnly);
            if (field is not null)
            {
                return field;
            }
        }
        return null;
    }

    private static PropertyInfo? GetPropertyUpHierarchy(Type? type, string name)
    {
        for (; type is not null; type = type.BaseType)
        {
            var property = type.GetProperty(name, InstanceAny | BindingFlags.DeclaredOnly);
            if (property is not null)
            {
                return property;
            }
        }
        return null;
    }
}
