using System.Activities;

namespace Super.Extensions;

public static partial class SuperExtensions
{
    /// <summary>
    /// Registers the extension dispatcher on a <see cref="WorkflowApplication"/>. Pass the same root
    /// activity the application was created with so the dispatcher can map records to activities.
    /// </summary>
    public static WorkflowApplication UseSuperExtensions(this WorkflowApplication app, Activity root, bool captureArguments = false)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.Extensions.Add(new ExtensionDispatchTrackingParticipant(root, captureArguments));
        return app;
    }

    /// <summary>
    /// Registers the extension dispatcher on a <see cref="WorkflowInvoker"/>. Pass the same root
    /// activity the invoker was created with.
    /// </summary>
    public static WorkflowInvoker UseSuperExtensions(this WorkflowInvoker invoker, Activity root, bool captureArguments = false)
    {
        ArgumentNullException.ThrowIfNull(invoker);
        invoker.Extensions.Add(new ExtensionDispatchTrackingParticipant(root, captureArguments));
        return invoker;
    }
}
