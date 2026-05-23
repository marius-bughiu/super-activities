using System.Activities;
using Super.Activities;
using Super.Extensions;

namespace Super.Extensions.Tests;

/// <summary>Test extension that records every before/after event and the property value it observed.</summary>
public sealed class RecordingExtension : IActivityExtension
{
    public readonly List<(string Name, string State, int MaxRetries)> Events = new();

    public string Name => "Recording";

    public Type DeclaringType => typeof(RecordingExtension);

    public string XmlNamespace => "clr-namespace:Super.Extensions.Tests;assembly=Super.Extensions.Tests";

    public bool AppliesTo(Activity activity) => activity is WriteMessage;

    public IReadOnlyList<ExtensionPropertyDefinition> Properties { get; } = new[]
    {
        new ExtensionPropertyDefinition("MaxRetries", typeof(int))
        {
            DefaultValue = 0,
            Category = "Reliability",
            Description = "Retry count.",
        },
    };

    public void OnBeforeExecute(ExtensionExecutionContext context)
        => Events.Add((context.Activity.Name, context.State, context.GetProperty<int>("MaxRetries")));

    public void OnAfterExecute(ExtensionExecutionContext context)
        => Events.Add((context.Activity.Name, context.State, context.GetProperty<int>("MaxRetries")));
}
