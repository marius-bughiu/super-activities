using System.Activities;

namespace Super.Extensions;

/// <summary>
/// Central registry and entry point for the extension framework. Plugins register an
/// <see cref="IActivityExtension"/> here; design-time and runtime wiring live in the partial files.
/// </summary>
public static partial class SuperExtensions
{
    private static readonly List<IActivityExtension> _extensions = new();
    private static readonly object _gate = new();

    public static void Register(IActivityExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        lock (_gate)
        {
            if (_extensions.Any(e => e.Name == extension.Name))
            {
                throw new InvalidOperationException($"An extension named '{extension.Name}' is already registered.");
            }
            _extensions.Add(extension);
        }
    }

    public static void Clear()
    {
        lock (_gate)
        {
            _extensions.Clear();
        }
    }

    public static IReadOnlyList<IActivityExtension> All
    {
        get
        {
            lock (_gate)
            {
                return _extensions.ToArray();
            }
        }
    }

    /// <summary>Registered extensions that apply to the given activity.</summary>
    public static IEnumerable<IActivityExtension> ForActivity(Activity activity)
    {
        foreach (var ext in All)
        {
            if (ext.AppliesTo(activity))
            {
                yield return ext;
            }
        }
    }
}
