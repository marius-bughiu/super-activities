using System.Activities.DesignViewModels;

namespace Super.AppInsights.Activities.ViewModels;

/// <summary>
/// Modern Studio designer view-model for <see cref="TrackEvent"/>. Property names/types must match the
/// activity's properties.
/// </summary>
public class TrackEventViewModel : DesignPropertiesViewModel
{
    public TrackEventViewModel(IDesignServices services) : base(services)
    {
    }

    // Note: TrackEvent.Properties is auto-rendered by the base view-model. It is intentionally NOT
    // declared here because DesignPropertiesViewModel already defines a member named "Properties";
    // shadowing it would break the designer.

    /// <summary>Matches <see cref="TrackEvent.EventName"/>.</summary>
    public DesignInArgument<string> EventName { get; set; } = new DesignInArgument<string>();

    protected override void InitializeModel()
    {
        base.InitializeModel();

        EventName.DisplayName = "Event Name";
        EventName.Tooltip = "Name of the custom event to send.";
        EventName.IsRequired = true;
        EventName.IsPrincipal = true;
        EventName.OrderIndex = 0;
    }
}
