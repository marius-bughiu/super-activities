using System.Activities;
using System.Activities.Tracking;

namespace Super.Extensions;

/// <summary>
/// Observes every activity state transition and dispatches before/after hooks to the registered
/// extensions that apply to each activity. Records carry only activity identity (the activity
/// definition reference is internal to the runtime), so we map <c>record.Activity.Id</c> back to the
/// <see cref="Activity"/> using a tree map built from <see cref="WorkflowInspectionServices"/>.
/// </summary>
internal sealed class ExtensionDispatchTrackingParticipant : TrackingParticipant
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyArguments =
        new Dictionary<string, object?>();

    private readonly Activity _root;
    private readonly bool _captureArguments;
    private readonly object _gate = new();
    private Dictionary<string, Activity>? _byId;

    public ExtensionDispatchTrackingParticipant(Activity root, bool captureArguments)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _captureArguments = captureArguments;
        if (captureArguments)
        {
            TrackingProfile = BuildCaptureProfile();
        }
    }

    protected override void Track(TrackingRecord record, TimeSpan timeout)
    {
        if (record is not ActivityStateRecord asr)
        {
            return;
        }

        var map = EnsureMap();
        if (!map.TryGetValue(asr.Activity.Id, out var activity))
        {
            return;
        }

        bool isBefore = string.Equals(asr.State, ActivityStates.Executing, StringComparison.Ordinal);
        var arguments = ReadArguments(asr);

        foreach (var ext in SuperExtensions.ForActivity(activity!))
        {
            try
            {
                var ctx = new ExtensionExecutionContext(asr.Activity, asr.State, asr.InstanceId, activity!, ext, arguments);
                if (isBefore)
                {
                    ext.OnBeforeExecute(ctx);
                }
                else
                {
                    ext.OnAfterExecute(ctx);
                }
            }
            catch (Exception ex)
            {
                SuperRuntime.Log($"extension '{ext.Name}' threw for '{asr.Activity.Name}': {ex.Message}", System.Diagnostics.TraceEventType.Warning);
            }
        }
    }

    private IReadOnlyDictionary<string, object?> ReadArguments(ActivityStateRecord asr)
    {
        if (!_captureArguments)
        {
            return EmptyArguments;
        }

        try
        {
            var dict = new Dictionary<string, object?>();
            foreach (var kvp in asr.Arguments)
            {
                dict[kvp.Key] = kvp.Value;
            }
            return dict;
        }
        catch
        {
            return EmptyArguments;
        }
    }

    private Dictionary<string, Activity> EnsureMap()
    {
        if (_byId is not null)
        {
            return _byId;
        }

        lock (_gate)
        {
            if (_byId is null)
            {
                var map = new Dictionary<string, Activity>(StringComparer.Ordinal);
                BuildMap(_root, map, new HashSet<Activity>());
                _byId = map;
            }
        }
        return _byId;
    }

    private static void BuildMap(Activity activity, Dictionary<string, Activity> map, HashSet<Activity> visited)
    {
        if (activity is null || !visited.Add(activity))
        {
            return;
        }

        if (!string.IsNullOrEmpty(activity.Id))
        {
            map[activity.Id] = activity;
        }

        foreach (var child in WorkflowInspectionServices.GetActivities(activity))
        {
            BuildMap(child, map, visited);
        }
    }

    private static TrackingProfile BuildCaptureProfile()
    {
        var query = new ActivityStateQuery { ActivityName = "*" };
        query.States.Add("*");
        query.Arguments.Add("*");
        query.Variables.Add("*");
        return new TrackingProfile { Queries = { query } };
    }
}
