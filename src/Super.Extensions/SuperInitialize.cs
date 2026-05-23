using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;

namespace Super.Extensions;

/// <summary>
/// Place this as the first activity in your process's Main. At runtime it captures the project settings,
/// auto-registers all installed extensions, and attaches the dispatcher to the live executor — so every
/// subsequent activity is reported to those extensions, even under the UiPath Robot (which never calls
/// our wire-up). One activity in Main covers the whole run, including non-isolated invoked workflows.
/// </summary>
[DisplayName(DefaultDisplayName)]
[Description(MagicLabel)]
public sealed class SuperInitialize : CodeActivity
{
    internal const string DefaultDisplayName = "Super Initialize";

    /// <summary>The witty bit shown on the activity card / tooltip.</summary>
    public const string MagicLabel = "Step right up — this is where the magic starts.";

    public SuperInitialize()
    {
        DisplayName = DefaultDisplayName;
    }

    protected override void Execute(CodeActivityContext context)
    {
        var runtime = context.GetExtension<IExecutorRuntime>();
        SuperRuntime.Host = runtime;
        if (runtime?.Settings is not null)
        {
            SuperRuntime.ProjectSettings = runtime.Settings;
        }

        ExtensionDiscovery.RegisterDiscovered();

        // Capture argument values only if some registered extension actually wants them.
        var captureArguments = SuperExtensions.All.Any(e => e.CapturesArguments);
        SuperExtensions.AttachToCurrentWorkflow(context, captureArguments);
    }
}
