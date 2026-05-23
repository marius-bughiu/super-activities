using System.Activities.DesignViewModels;

namespace Super.Activities.ViewModels;

/// <summary>
/// Modern Studio designer view-model for <see cref="WriteMessage"/>. Property names/types must match
/// the activity's properties.
/// </summary>
public class WriteMessageViewModel : DesignPropertiesViewModel
{
    public WriteMessageViewModel(IDesignServices services) : base(services)
    {
    }

    /// <summary>Matches <see cref="WriteMessage.Message"/> (an InArgument, so DesignInArgument).</summary>
    public DesignInArgument<string> Message { get; set; } = new DesignInArgument<string>();

    protected override void InitializeModel()
    {
        base.InitializeModel();

        Message.DisplayName = "Message";
        Message.Tooltip = "The text to write.";
        Message.IsRequired = true;
        Message.IsPrincipal = true;
        Message.OrderIndex = 0;
    }
}
