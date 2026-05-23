using System.Activities;
using System.Xaml;

namespace Super.Extensions;

/// <summary>
/// Reads and writes extension property values, which are stored as attached properties on the
/// activity definition object via <see cref="AttachablePropertyServices"/>. This is the single
/// source of truth shared by the design-time property grid, the XAML serializer, and the runtime hook.
/// </summary>
public static class ActivityExtensionData
{
    internal static AttachableMemberIdentifier Identifier(IActivityExtension ext, string property)
        => new(ext.DeclaringType, property);

    public static void Set(Activity activity, IActivityExtension ext, string property, object? value)
        => AttachablePropertyServices.SetProperty(activity, Identifier(ext, property), value);

    public static bool TryGet(Activity activity, IActivityExtension ext, string property, out object? value)
        => AttachablePropertyServices.TryGetProperty(activity, Identifier(ext, property), out value);

    public static bool Remove(Activity activity, IActivityExtension ext, string property)
        => AttachablePropertyServices.RemoveProperty(activity, Identifier(ext, property));

    /// <summary>Lists every attached property currently set on the activity (across all extensions).</summary>
    public static IReadOnlyList<AttachedProperty> List(object activity)
    {
        int count = AttachablePropertyServices.GetAttachedPropertyCount(activity);
        if (count == 0)
        {
            return Array.Empty<AttachedProperty>();
        }

        var buffer = new KeyValuePair<AttachableMemberIdentifier, object>[count];
        AttachablePropertyServices.CopyPropertiesTo(activity, buffer, 0);

        var result = new AttachedProperty[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = new AttachedProperty(buffer[i].Key.DeclaringType, buffer[i].Key.MemberName, buffer[i].Value);
        }
        return result;
    }

    public readonly record struct AttachedProperty(Type? DeclaringType, string MemberName, object? Value);
}
