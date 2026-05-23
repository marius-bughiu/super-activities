using System.Activities;

namespace Super.Extensions;

public static partial class SuperExtensions
{
    /// <summary>
    /// Attaches the extension dispatcher to the workflow that is currently executing, by reaching into
    /// the live CoreWF executor via reflection. Call this from inside a running activity (e.g. an
    /// "initialize" activity placed at the start of the workflow) when the host does not call
    /// <see cref="UseSuperExtensions(WorkflowApplication, Activity, bool)"/> itself — notably the UiPath
    /// Robot. Every activity scheduled after this point is dispatched to the registered extensions.
    /// </summary>
    public static void AttachToCurrentWorkflow(ActivityContext context, bool captureArguments = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        var root = LiveExecutionHook.GetRootActivity(context);
        var participant = new ExtensionDispatchTrackingParticipant(root, captureArguments);
        LiveExecutionHook.AddTrackingParticipant(context, participant);
    }
}
