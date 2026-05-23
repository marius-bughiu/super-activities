using System.Activities.DesignViewModels;

namespace Super.Extensions.ViewModels;

/// <summary>
/// Modern Studio designer view-model for <see cref="SuperInitialize"/>. Activity-mapped property
/// names/types must match the activity; extra display-only properties (e.g. the info note) are allowed.
/// </summary>
public class SuperInitializeViewModel : DesignPropertiesViewModel
{
    public SuperInitializeViewModel(IDesignServices services) : base(services)
    {
    }

    /// <summary>Display-only info note rendered as a TextBlock widget (not an activity property).</summary>
    public DesignProperty<string> Note { get; set; } = new DesignProperty<string>();

    protected override void InitializeModel()
    {
        base.InitializeModel();

        Note.OrderIndex = 0;
        Note.IsVisible = true;
        Note.Widget = new TextBlockWidget
        {
            Level = TextBlockWidgetLevel.Info,
            Multiline = true,
        };
        Note.Value = "Step right up — this is where the magic starts. " +
                     "Drop **Super Initialize** at the top of Main and your extensions run for the whole process.";
    }
}
