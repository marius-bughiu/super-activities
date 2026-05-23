using System.Activities;

namespace Super.Activities;

/// <summary>
/// Minimal sample activity used to exercise the extension pipeline.
/// </summary>
public sealed class WriteMessage : CodeActivity
{
    public InArgument<string> Message { get; set; } = new();

    protected override void Execute(CodeActivityContext context)
    {
        Console.WriteLine(context.GetValue(Message));
    }
}
