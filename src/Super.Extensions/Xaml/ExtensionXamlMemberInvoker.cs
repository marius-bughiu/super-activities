using System.Xaml;
using System.Xaml.Schema;

namespace Super.Extensions.Xaml;

/// <summary>
/// Reads/writes a dynamic attachable member's value through <see cref="AttachablePropertyServices"/>,
/// so XAML serialization and deserialization use the same store as design time and runtime.
/// </summary>
internal sealed class ExtensionXamlMemberInvoker : XamlMemberInvoker
{
    private readonly AttachableMemberIdentifier _id;
    private readonly object? _default;

    public ExtensionXamlMemberInvoker(XamlMember member, IActivityExtension extension, ExtensionPropertyDefinition definition)
        : base(member)
    {
        _id = new AttachableMemberIdentifier(extension.DeclaringType, definition.Name);
        _default = definition.DefaultValue;
    }

    public override object GetValue(object instance)
        => AttachablePropertyServices.TryGetProperty(instance, _id, out object? value) ? value! : _default!;

    public override void SetValue(object instance, object value)
        => AttachablePropertyServices.SetProperty(instance, _id, value);
}
