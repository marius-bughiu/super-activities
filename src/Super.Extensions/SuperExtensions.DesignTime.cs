using System.Activities;
using System.ComponentModel;

namespace Super.Extensions;

public static partial class SuperExtensions
{
    private static bool _designTimeEnabled;
    private static readonly object _designTimeGate = new();

    /// <summary>
    /// Registers the extension <see cref="TypeDescriptionProvider"/> so extension properties appear in
    /// any <see cref="TypeDescriptor"/>-driven property grid. Idempotent.
    /// </summary>
    public static void EnableDesignTime()
    {
        lock (_designTimeGate)
        {
            if (_designTimeEnabled)
            {
                return;
            }

            var parent = TypeDescriptor.GetProvider(typeof(Activity));
            TypeDescriptor.AddProvider(new ExtensionTypeDescriptionProvider(parent), typeof(Activity));
            _designTimeEnabled = true;
        }
    }
}
